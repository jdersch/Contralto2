/*

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

using Contralto.Logging;
using System.Collections.Specialized;
using System.Reflection;

namespace Contralto
{
    /// <summary>
    /// The configuration of the Alto to emulate
    /// </summary>
    public enum SystemType
    {
        /// <summary>
        /// Alto I System with 1K ROM, 1K RAM
        /// </summary>
        AltoI,

        /// <summary>
        /// Alto II XM System with the standard 1K ROM, 1K RAM
        /// </summary>
        OneKRom,

        /// <summary>
        /// Alto II XM System with 2K ROM, 1K RAM
        /// </summary>
        TwoKRom,

        /// <summary>
        /// Alto II XM System with 3K RAM
        /// </summary>
        ThreeKRam,
    }

    public enum PacketInterfaceType
    {
        /// <summary>
        /// Encapsulate frames inside raw ethernet frames on the host interface.
        /// Requires PCAP.
        /// </summary>
        EthernetEncapsulation,

        /// <summary>
        /// Encapsulate frames inside UDP datagrams on the host interface.
        /// </summary>
        UDPEncapsulation,

        /// <summary>
        /// No encapsulation; sent packets are dropped on the floor and no packets are received.
        /// </summary>
        None,
    }

    public enum AlternateBootType
    {
        None,
        Disk,
        Ethernet,
    }

    public enum PlatformType
    {
        Windows,
        Unix
    }

    public class StartupOptions
    {
        public static string ConfigurationFile = String.Empty;
        public static string ScriptFile = String.Empty;
        public static string RomPath = String.Empty;
    }

    /// <summary>
    /// Encapsulates user-configurable settings.  To be enhanced.
    /// </summary>
    public class Configuration
    {
        static Configuration()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    Platform = PlatformType.Unix;
                    break;

                default:
                    Platform = PlatformType.Windows;
                    break;
            }

            // See if PCap is available.
            TestPCap();
        }

        public Configuration() 
        {
            // Initialize things to defaults.
            HostAddress = 0x22;
            HostPacketInterfaceName = "<none>";
            HostPacketInterfaceType = PacketInterfaceType.None;

            BootAddress = 0;
            BootFile = 0;

            SystemType = SystemType.OneKRom;

            FullScreenStretch = true;
            DisplayScale = 1;
            SlowPhosphorSimulation = true;
            ThrottleSpeed = true;

            AudioDACCapturePath = String.Empty;
            PrintOutputPath = String.Empty;
            TridentImages = new StringCollection();
            
        }

        public Configuration(Configuration config)
        {
            AllowKioskExit = config.AllowKioskExit;
            AlternateBootType = config.AlternateBootType;
            AudioDACCapturePath = config.AudioDACCapturePath;
            BootAddress = config.BootAddress;
            BootFile = config.BootFile;
            DisplayScale = config.DisplayScale;
            Drive0Image = config.Drive0Image;
            Drive1Image = config.Drive1Image;
            EnableAudioDAC = config.EnableAudioDAC;
            EnableAudioDACCapture = config.EnableAudioDACCapture;
            EnablePrinting = config.EnablePrinting;
            FullScreenStretch = config.FullScreenStretch;
            HostAddress = config.HostAddress;
            HostPacketInterfaceName = config.HostPacketInterfaceName;
            HostPacketInterfaceType = config.HostPacketInterfaceType;
            KioskMode = config.KioskMode;
            PageRasterOffsetX = config.PageRasterOffsetX;
            PageRasterOffsetY = config.PageRasterOffsetY;
            PauseWhenNotActive = config.PauseWhenNotActive;
            PrintOutputPath = config.PrintOutputPath;
            ReversePageOrder = config.ReversePageOrder;
            SlowPhosphorSimulation = config.SlowPhosphorSimulation;
            SystemType = config.SystemType;
            ThrottleSpeed = config.ThrottleSpeed;

            TridentImages = new StringCollection();
            foreach (string? imageName in config.TridentImages)
            {
                TridentImages.Add(imageName);
            }
        }

        public Configuration(bool loadConfiguration) : this()
        {
            if (loadConfiguration)
            {
                try
                {
                    ReadConfiguration();
                }
                catch
                {
                    Log.Write(LogType.Warning, LogComponent.Configuration, "Warning: unable to load configuration.  Assuming default settings.");
                }
            }

            // Special case: On first startup, AlternateBoot will come back as "None" which
            // is an invalid UI setting; default to Ethernet in this case.
            if (AlternateBootType == AlternateBootType.None)
            {
                AlternateBootType = AlternateBootType.Ethernet;
            }

            //
            // If configuration specifies fewer than 8 Trident disks, we need to pad the list out to 8 with empty entries.
            //
            if (TridentImages == null)
            {
                TridentImages = new StringCollection();
            }

            int start = TridentImages.Count;
            for (int i = start; i < 8; i++)
            {
                TridentImages.Add(String.Empty);
            }
        }

        /// <summary>
        /// Global config: what kind of system we're running on.
        /// </summary>
        public static PlatformType Platform;

        /// <summary>
        /// Global config: Whether any packet interfaces are available on the host
        /// </summary>
        public static bool HostRawEthernetInterfacesAvailable;

        /// <summary>
        /// The components to enable debug logging for.
        /// </summary>
        public static LogComponent LogComponents;

        /// <summary>
        /// The types of logging to enable.
        /// </summary>
        public static LogType LogTypes;

        /// <summary>
        /// The type of Alto system to emulate
        /// </summary>
        public SystemType SystemType;

        /// <summary>
        /// The currently loaded image for Drive 0
        /// </summary>
        public string? Drive0Image;

        /// <summary>
        /// The currently loaded image for Drive 1
        /// </summary>
        public string? Drive1Image;

        /// <summary>
        /// The currently loaded images for the Trident controller.
        /// </summary>
        public StringCollection TridentImages;

        /// <summary>
        /// The Ethernet host address for this Alto
        /// </summary>
        public byte HostAddress;

        /// <summary>
        /// The name of the Ethernet adaptor on the emulator host to use for Ethernet emulation
        /// </summary>
        public string HostPacketInterfaceName;

        /// <summary>
        /// The type of interface to use to host networking.
        /// </summary>
        public PacketInterfaceType HostPacketInterfaceType;

        /// <summary>
        /// The type of Alternate Boot to apply
        /// </summary>
        public AlternateBootType AlternateBootType;

        /// <summary>
        /// The address to boot at reset for a disk alternate boot
        /// </summary>
        public ushort BootAddress;

        /// <summary>
        /// The file to boot at reset for an ethernet alternate boot
        /// </summary>
        public ushort BootFile;

        /// <summary>
        /// Whether to cap execution speed at native execution speed or not.
        /// </summary>
        public bool ThrottleSpeed;

        /// <summary>
        /// Whether to stretch the display in fullscreen mode to take up as much space as possible.
        /// </summary>
        public bool FullScreenStretch;

        /// <summary>
        /// Whether to pause emulation when the ContrAlto window loses focus or not.
        /// </summary>
        public bool PauseWhenNotActive;

        /// <summary>
        /// An integer scaling factor to apply to the display (when not running in full-screen mode)
        /// </summary>
        public int DisplayScale;

        /// <summary>
        /// Enables or disables slow phosphor emulation
        /// </summary>
        public bool SlowPhosphorSimulation;

        /// <summary>
        /// Whether to enable the DAC used for the Smalltalk music system.
        /// </summary>
        public bool EnableAudioDAC;

        /// <summary>
        /// Whether to enable capture of the DAC output to file.
        /// </summary>
        public bool EnableAudioDACCapture;

        /// <summary>
        /// The path to store DAC capture (if enabled).
        /// </summary>
        public string AudioDACCapturePath;

        /// <summary>
        /// Whether to enable printing via the Orbit / DoverROS interface.
        /// </summary>
        public bool EnablePrinting;

        /// <summary>
        /// Path for print output.
        /// </summary>
        public string PrintOutputPath;

        /// <summary>
        /// Whether to reverse the page order when printing.
        /// </summary>
        public bool ReversePageOrder;

        /// <summary>
        /// Allows adjusting the position of the raster on the printed page.
        /// </summary>
        public int PageRasterOffsetX;

        /// <summary>
        /// Allows adjusting the position of the raster on the printed page.
        /// </summary>
        public int PageRasterOffsetY;

        /// <summary>
        /// When set, enables "kiosk" mode, which forces fullscreen (with no means to leave it)
        /// and disables most hotkeys, to prevent tampering with or exiting the emulator.
        /// This setting is only configurable via a manually created config file.
        /// </summary>
        public bool KioskMode;

        /// <summary>
        /// When set, allows users to exit from the emulator by hitting Ctrl+Alt+X.  Use if
        /// running the emulator from an environment where it's safe to exit (for example, if
        /// launched from a menu system or something similar.)
        /// This setting is only configurable via a manually created config file.
        /// </summary>
        public bool AllowKioskExit;


        public static string GetAltoIRomPath(string romFileName)
        {
            if (string.IsNullOrEmpty(StartupOptions.RomPath))
            {
                return Path.Combine("ROM", "AltoI", romFileName);
            }
            else
            {
                return Path.Combine(StartupOptions.RomPath, "AltoI", romFileName);
            }
        }

        public static string GetAltoIIRomPath(string romFileName)
        {
            if (string.IsNullOrEmpty(StartupOptions.RomPath))
            {
                return Path.Combine("ROM", "AltoII", romFileName);
            }
            else
            {
                return Path.Combine(StartupOptions.RomPath, "AltoII", romFileName);
            }
        }

        public static string GetRomPath(string romFileName)
        {
            if (string.IsNullOrEmpty(StartupOptions.RomPath))
            {
                return Path.Combine("ROM", romFileName);
            }
            else
            {
                return Path.Combine(StartupOptions.RomPath, romFileName);
            }
        }

        /// <summary>
        /// Reads the current configuration file from the appropriate place.
        /// </summary>
        public void ReadConfiguration()
        {
            string? configFilePath = null;

            if (!string.IsNullOrWhiteSpace(StartupOptions.ConfigurationFile))
            {
                configFilePath = StartupOptions.ConfigurationFile;
            }
            else
            {
                // No config file specified, default.
                configFilePath = "Contralto.cfg";
            }

            //
            // Check that the configuration file exists.
            // If not, we will warn the user and use default settings.
            //
            if (!File.Exists(configFilePath))
            {
                Log.Write(LogType.Warning, LogComponent.Configuration, "Configuration file {0} does not exist or cannot be accessed.  Using default settings.", configFilePath);
                return;
            }

            using (StreamReader configStream = new StreamReader(configFilePath))
            {
                //
                // Config file consists of text lines containing name / value pairs:
                //      <Name>=<Value>
                // Whitespace is ignored.
                //
                int lineNumber = 0;
                while (!configStream.EndOfStream)
                {
                    lineNumber++;
                    string? line = configStream.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(line))
                    {
                        // Empty line, ignore.
                        continue;
                    }

                    if (line.StartsWith("#"))
                    {
                        // Comment to EOL, ignore.
                        continue;
                    }

                    // Find the '=' separating tokens and ensure there are just two.
                    string[] tokens = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                    if (tokens.Length < 2)
                    {
                        Log.Write(LogType.Warning, LogComponent.Configuration, "{0} line {1}: Invalid syntax.", configFilePath, lineNumber);
                        continue;
                    }

                    string parameter = tokens[0].Trim();
                    string value = tokens[1].Trim();

                    // Reflect over the public properties in this class and see if the parameter matches one of them
                    // If not, it's an error, if it is then we attempt to coerce the value to the correct type.
                    FieldInfo[] info = typeof(Configuration).GetFields(BindingFlags.Public | BindingFlags.Instance);

                    bool bMatch = false;
                    foreach (FieldInfo field in info)
                    {
                        // Case-insensitive compare.
                        if (field.Name.ToLowerInvariant() == parameter.ToLowerInvariant())
                        {
                            bMatch = true;

                            //
                            // Switch on the type of the field and attempt to convert the value to the appropriate type.
                            // At this time we support only strings and integers.
                            //
                            try
                            {
                                switch (field.FieldType.Name)
                                {
                                    case "Int32":
                                        {
                                            int v = Convert.ToInt32(value, 8);
                                            field.SetValue(this, v);
                                        }
                                        break;

                                    case "UInt16":
                                        {
                                            UInt16 v = Convert.ToUInt16(value, 8);
                                            field.SetValue(this, v);
                                        }
                                        break;

                                    case "Byte":
                                        {
                                            byte v = Convert.ToByte(value, 8);
                                            field.SetValue(this, v);
                                        }
                                        break;

                                    case "String":
                                        {
                                            field.SetValue(this, value);
                                        }
                                        break;

                                    case "Boolean":
                                        {
                                            bool v = bool.Parse(value);
                                            field.SetValue(this, v);
                                        }
                                        break;

                                    case "SystemType":
                                        {
                                            field.SetValue(this, Enum.Parse(typeof(SystemType), value, true));
                                        }
                                        break;

                                    case "PacketInterfaceType":
                                        {
                                            field.SetValue(this, Enum.Parse(typeof(PacketInterfaceType), value, true));
                                        }
                                        break;

                                    case "AlternateBootType":
                                        {
                                            field.SetValue(this, Enum.Parse(typeof(AlternateBootType), value, true));
                                        }
                                        break;

                                    case "LogType":
                                        {
                                            field.SetValue(this, Enum.Parse(typeof(LogType), value, true));
                                        }
                                        break;

                                    case "LogComponent":
                                        {
                                            field.SetValue(this, Enum.Parse(typeof(LogComponent), value, true));
                                        }
                                        break;

                                    case "StringCollection":
                                        {
                                            // value is expected to be a comma-delimited set.
                                            StringCollection sc = new StringCollection();
                                            string[] strings = value.Split(',');

                                            foreach (string s in strings)
                                            {
                                                sc.Add(s);
                                            }

                                            field.SetValue(this, sc);
                                        }
                                        break;
                                }
                            }
                            catch
                            {
                                Log.Write(LogType.Warning, LogComponent.Configuration, "{0} line {1}: Value '{2}' is invalid for parameter '{3}'.", configFilePath, lineNumber, value, parameter);
                            }
                        }
                    }

                    if (!bMatch)
                    {
                        Log.Write(LogType.Warning, LogComponent.Configuration, "{0} line {1}: Unknown configuration parameter '{2}'.", configFilePath, lineNumber, parameter);
                    }
                }
            }
        }

        /// <summary>
        /// Commits the current configuration back to the configuration file.
        /// </summary>
        public void WriteConfiguration()
        {

            if (!string.IsNullOrWhiteSpace(StartupOptions.ConfigurationFile))
            {
                // If a config file was explicitly specified at startup we will not overwrite it.
                return;
            }

            string configFilePath = "Contralto.cfg";

            List<string> configLines = new List<string>();
            using (StreamReader sr = new StreamReader(configFilePath))
            {
                while (!sr.EndOfStream)
                {
                    string? line = sr.ReadLine();

                    // This can never actually be null, this is here to make static nullability checks happy.
                    if (line != null)
                    {
                        configLines.Add(line);
                    }
                }
            }

            // Reflect over the public properties in this class and write each one out to the file.
            // We look for a matching entry in the config file and overwrite that if so, otherwise it just
            // gets appended to the end of the file.
            FieldInfo[] info = typeof(Configuration).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in info)
            { 

                int paramLine = configLines.FindIndex(s => s.StartsWith(field.Name, StringComparison.OrdinalIgnoreCase));

                // Apply special formatting for integer types (write out as octal)
                string? outputValue = null;
                switch (field.FieldType.Name)
                {
                    case "Int32":
                        {
                            outputValue = Convert.ToString((Int32)field.GetValue(this)!, 8);
                        }
                        break;

                    case "UInt16":
                        {
                            outputValue = Convert.ToString((UInt16)field.GetValue(this)!, 8);
                        }
                        break;

                    case "Byte":
                        {
                            outputValue = Convert.ToString((byte)field.GetValue(this)!, 8);
                        }
                        break;

                    case "StringCollection":
                        {
                            // value is expected to be a comma-delimited set.
                            StringCollection? strings = field.GetValue(this) as StringCollection;
                            if (strings == null)
                            {
                                break;
                            }

                            foreach (string? s in strings)
                            {
                                if (outputValue == null)
                                {
                                    outputValue = s;
                                }
                                else
                                {
                                    outputValue = $"{outputValue}, {s}";
                                }
                            }
                        }
                        break;

                    default:
                        outputValue = field.GetValue(this)?.ToString();
                        break;

                }

                if (outputValue == null)
                {
                    // No value was specified, nothing to write, but kill the existing line if there is one.
                    if (paramLine != -1)
                    {
                        configLines.RemoveAt(paramLine);
                    }
                    continue;
                }

                string outputLine = $"{field.Name} = {outputValue}";

                if (paramLine == -1)
                {
                    // Add the line to the end
                    configLines.Add(outputLine);
                }
                else
                {
                    configLines[paramLine] = outputLine;
                }
            }

            using (StreamWriter sw = new StreamWriter(configFilePath))
            {
                foreach(string line in configLines)
                {
                    sw.WriteLine(line);
                }
            }
        }

        private static void TestPCap()
        {         
            /*
            // Just try enumerating interfaces, if this fails for any reason we assume
            // PCap is not properly installed.
            try
            {
                SharpPcap.CaptureDeviceList devices = SharpPcap.CaptureDeviceList.Instance;
                Configuration.HostRawEthernetInterfacesAvailable = true;
            }
            catch
            {
                Configuration.HostRawEthernetInterfacesAvailable = false;
            } */        
        }


    }

}
