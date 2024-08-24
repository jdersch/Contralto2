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

using System.Diagnostics.CodeAnalysis;

namespace Contralto.CPU
{
    /// <summary>
    /// Maintains a set of Control ROM images dumped from real Alto hardware.
    /// </summary>
    public class ControlROM
    {
        public ControlROM(Configuration configuration)
        {
            Init(configuration);
        }

        private void Init(Configuration configuration)
        {
            if (configuration.SystemType == SystemType.AltoI)
            {
                LoadConstants(_constantRomsAltoI, true);
                LoadACSource(_acSourceRoms, true);
            }
            else
            {
                LoadConstants(_constantRomsAltoII, false);
                LoadACSource(_acSourceRoms, true);
            }
        }

        public ushort[] ConstantROM
        {
            get { return _constantRom; }
        }

        public byte[] ACSourceROM
        {
            get { return _acSourceRom; }
        }

        private void LoadConstants(RomFile[] romInfo, bool flip)
        {
            _constantRom = new ushort[256];

            foreach (RomFile file in romInfo)
            {
                //
                // Each file contains 256 bytes, each byte containing one nybble in the low 4 bits.
                //
                using (FileStream fs = new FileStream(file.Filename, FileMode.Open, FileAccess.Read))
                {
                    int length = (int)fs.Length;
                    if (length != 256)
                    {
                        throw new InvalidOperationException("ROM file should be 256 bytes in length");
                    }

                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, (int)fs.Length);

                    // OR in the data
                    for (int i = 0; i < length; i++)
                    {
                        if (flip)
                        {
                            _constantRom[file.StartingAddress + i] |= (ushort)((DataMapConstantRom(~data[AddressMapConstantRom(i)]) & 0xf) << file.BitPosition);
                        }
                        else
                        {
                            _constantRom[file.StartingAddress + i] |= (ushort)((DataMapConstantRom(data[AddressMapConstantRom(i)]) & 0xf) << file.BitPosition);
                        }
                    }
                }
            }

            // And invert all bits
            for (int i = 0; i < _constantRom.Length; i++)
            {
               _constantRom[i] = (ushort)((~_constantRom[i]) & 0xffff);
            } 
        }

        private void LoadACSource(RomFile romInfo, bool reverseBits)
        {
            _acSourceRom = new byte[256];
            
            using (FileStream fs = new FileStream(romInfo.Filename, FileMode.Open, FileAccess.Read))
            {
                int length = (int)fs.Length;
                if (length != 256)
                {
                    throw new InvalidOperationException("ROM file should be 256 bytes in length");
                }
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, (int)fs.Length);

                // Copy in the data, modifying the address as required.
                for (int i = 0; i < length; i++)
                {
                    if (reverseBits)
                    {
                        _acSourceRom[i] = (byte)((~DataMapConstantRom(data[AddressMapACSourceRom(i)])) & 0xf);
                    }
                    else
                    {
                        _acSourceRom[i] = (byte)((~data[AddressMapACSourceRom(i)]) & 0xf);
                    }
                }
            }
        }

        private int AddressMapConstantRom(int address)
        {            
            // Descramble the address bits as they are in no sane order.    
            // (See 05a_AIM.pdf, pg. 5 (Page 9 of the orginal docs))
            int[] addressMapping = { 7, 2, 1, 0, 3, 4, 5, 6 };            
  
            int mappedAddress = 0;

            for (int i = 0; i < addressMapping.Length; i++)
            {
                if ((address & (1 << i)) != 0)
                {
                    mappedAddress |= (1 << (addressMapping[i]));
                }
            }            
            return mappedAddress;
        }

        private int DataMapConstantRom(int data)
        {
            // Reverse bits 0-4.
            int mappedData = 0;

            for (int i = 0; i < 4; i++)
            {
                if ((data & (1 << i)) != 0)
                {
                    mappedData |= (1 << (3-i));
                }
            }

            return mappedData;
        }

        private int AddressMapACSourceRom(int data)
        {
            // Reverse bits 0-7.
            int mappedData = 0;

            for (int i = 0; i < 8; i++)
            {
                if ((data & (1 << i)) != 0)
                {
                    mappedData |= (1 << (7 - i));
                }
            }

            // And invert data lines
            return (~mappedData) & 0xff;
        }
       
        private static RomFile[] _constantRomsAltoI =
           {
                new RomFile(Configuration.GetAltoIRomPath("C0_23.BIN"), 0x000, 12),
                new RomFile(Configuration.GetAltoIRomPath("C1_23.BIN"), 0x000, 8),
                new RomFile(Configuration.GetAltoIRomPath("C2_23.BIN"), 0x000, 4),
                new RomFile(Configuration.GetAltoIRomPath("C3_23.BIN"), 0x000, 0),
            };

        private static RomFile[] _constantRomsAltoII =
            {
                new RomFile(Configuration.GetAltoIIRomPath("C0"), 0x000, 12),
                new RomFile(Configuration.GetAltoIIRomPath("C1"), 0x000, 8),
                new RomFile(Configuration.GetAltoIIRomPath("C2"), 0x000, 4),
                new RomFile(Configuration.GetAltoIIRomPath("C3"), 0x000, 0),
            };

        private RomFile _acSourceRoms = new RomFile(Configuration.GetRomPath("ACSOURCE.NEW"), 0x000, 0);

        private ushort[] _constantRom = null!;
        private byte[] _acSourceRom = null!;
    }
}
