﻿/*

Copyright (c) 2016-2020 Living Computers: Museum+Labs
Copyright (c) 2016-2024 Josh Dersch

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer 
       in the documentation and/or other materials provided with the distribution.
    3. Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote products derived from 
       this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED 
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS 
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System.Net;
using System.Net.Sockets;

using Contralto.Logging;
using System.Net.NetworkInformation;

namespace Contralto.IO
{    
    /// <summary>
    /// Implements the logic for encapsulating a 3mbit ethernet packet into/out of UDP datagrams.
    /// Sent packets are broadcast to the subnet.        
    /// </summary>
    public class UDPEncapsulation : IPacketEncapsulation
    {
        public UDPEncapsulation(string interfaceName)
        {
            _shutdown = false;

            // Try to set up UDP client.
            try
            {
                _udpClient = new UdpClient(_udpPort, AddressFamily.InterNetwork);
                _udpClient.EnableBroadcast = true;
                _udpClient.Client.Blocking = true;
                _udpClient.Client.MulticastLoopback = false;

                //
                // Grab the broadcast address for the interface so that we know what broadcast address to use
                // for our UDP datagrams.
                //
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

                IPInterfaceProperties? props = null;
                foreach(NetworkInterface nic in nics)
                {
                    if (nic.Name == interfaceName)
                    {
                        props = nic.GetIPProperties();
                        break;
                    }
                }

                if (props == null)
                {
                    throw new InvalidOperationException(String.Format("No interface matching description '{0}' was found.", interfaceName));
                }

                foreach(UnicastIPAddressInformation unicast in props.UnicastAddresses)
                {
                    // Find the first InterNetwork address for this interface and 
                    // go with it.
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        _thisIPAddress = unicast.Address;
                        _broadcastEndpoint = new IPEndPoint(GetBroadcastAddress(_thisIPAddress, unicast.IPv4Mask), _udpPort);
                        break;
                    }
                }
                
                if (_broadcastEndpoint == null)
                {
                    throw new InvalidOperationException(String.Format("No IPV4 network information was found for interface '{0}'.", interfaceName));
                }
                   
            }
            catch(Exception e)
            {
                Log.Write(LogType.Error, LogComponent.EthernetPacket,
                    "Error configuring UDP socket {0} for use with ContrAlto on interface {1}.  Ensure that the selected network interface is valid, configured properly, and that nothing else is using this port.",
                    _udpPort,
                    interfaceName);

                Log.Write(LogType.Error, LogComponent.EthernetPacket,
                    "Error was '{0}'.",
                    e.Message);

                _udpClient = null!;
            }
        }        

        public void RegisterReceiveCallback(ReceivePacketDelegate callback)
        {   
            // UDP connection could not be configured, can't receive anything.
            if (_udpClient == null)
            {
                return;
            }

            // Set up input
            _callback = callback;
            BeginReceive();
        }

        public void Shutdown()
        {
            if (_udpClient != null)
            {
                _udpClient.Close();
            }

            // Shut down the reciever thread.
            _shutdown = true;
            if (_receiveThread != null)
            {
                _receiveThread.Join();
            }
        }


        /// <summary>
        /// Sends an array of bytes over the ethernet as a 3mbit packet encapsulated in a 10mbit packet.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="hostId"></param>
        public void Send(ushort[] packet, int length)
        {
            // UDP could not be configured, drop the packet.
            if (_udpClient == null)
            {
                return;
            }

            // Sanity check.
            if (length < 1)
            {
                throw new InvalidOperationException("Raw packet data must contain at least two bytes for addressing.");
            }

            //
            // Outgoing packet contains 1 extra word (2 bytes) containing
            // the prepended packet length (one word)            
            byte[] packetBytes = new byte[length * 2 + 2];

            //
            // First two bytes include the length of the 3mbit packet; since 10mbit packets have a minimum length of 46 
            // bytes, and 3mbit packets have no minimum length this is necessary so the receiver can pull out the 
            // correct amount of data.
            //            
            packetBytes[0] = (byte)((length) >> 8);
            packetBytes[1] = (byte)(length);

            //
            // Do this annoying dance to stuff the ushorts into bytes because this is C#.
            //
            for (int i = 0; i < length; i++)
            {                
                packetBytes[i * 2 + 2] = (byte)(packet[i] >> 8);
                packetBytes[i * 2 + 3] = (byte)(packet[i]);
            }

            Log.Write(LogType.Verbose, LogComponent.HostNetworkInterface, "Sending packet via UDP; source {0} destination {1}, length {2} words.",
                Conversion.ToOctal(packetBytes[3]),
                Conversion.ToOctal(packetBytes[2]),
                length);

            _udpClient.Send(packetBytes, packetBytes.Length, _broadcastEndpoint);            
        }

        /// <summary>
        /// Begin receiving packets, forever.
        /// </summary>
        private void BeginReceive()
        {
            // Kick off receive thread.   
            _receiveThread = new Thread(ReceiveThread);
            _receiveThread.Start();
        }

        private void ReceiveThread()
        {
            Log.Write(LogComponent.HostNetworkInterface, "UDP Receiver thread started.");
            
            IPEndPoint groupEndPoint = new IPEndPoint(IPAddress.Any, _udpPort);            

            while (!_shutdown)
            {
                byte[] data = _udpClient.Receive(ref groupEndPoint);
                
                //
                // Sanitize the data (at least make sure the length is valid):
                //
                if (data.Length < 4)
                {
                    Log.Write(LogType.Verbose, LogComponent.HostNetworkInterface, "Invalid packet: Packet is fewer than 2 words long, dropping.");
                    continue;
                }

                // Drop our own UDP packets.
                if (!groupEndPoint.Address.Equals(_thisIPAddress))
                {
                    Log.Write(LogComponent.HostNetworkInterface, "Received UDP-encapsulated 3mbit packet.");
                    _callback?.Invoke(new System.IO.MemoryStream(data));
                }
            }
        }

        
        private IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastAddress);
        } 

        // Callback delegate for received data.
        private ReceivePacketDelegate? _callback;

        // Thread used for receive
        private Thread? _receiveThread;
        private bool _shutdown;

        // UDP port (TODO: make configurable?)
        private const int _udpPort = 42424;
        private UdpClient _udpClient;
        private IPEndPoint? _broadcastEndpoint;

        // The IP address (unicast address) of the interface we're using to send UDP datagrams.
        private IPAddress? _thisIPAddress;
    }   
}
