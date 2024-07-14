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

using Contralto.CPU;
using Contralto.Logging;
using Contralto.Memory;

namespace Contralto.IO
{
    /// <summary>
    /// Implements the Organ Keyboard interface used by the ST-74
    /// Music System.  Very little is known about the hardware at this time,
    /// so most of this is speculation or based on disassembly/reverse-engineering
    /// of the music system code.
    /// 
    /// This is currently a stub that implements the bare minimum to make the
    /// music system think there's a keyboard attached to the system.
    /// </summary>
    public class OrganKeyboard : IMemoryMappedDevice
    {
        public OrganKeyboard(AltoSystem system)
        {
            _system = system;
            Reset();
        }

        public void Reset()
        {
            //
            // Initialize keyboard registers.
            // Based on disassembly of the Nova code that drives the keyboard
            // interface, the top 6 bits are active low.
            //
            for (int i = 0; i < 16; i++)
            {                
                _keyData[i] = (ushort)(0xfc00);
            }
        }

        /// <summary>
        /// Reads a word from the specified address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="extendedMemory"></param>
        /// <returns></returns>
        public ushort Read(int address, TaskType task, bool extendedMemory)
        {

            Log.Write(LogType.Verbose, LogComponent.Organ, "Organ read from {0} by task {1} (bank {2}), Nova PC {3}",
                Conversion.ToOctal(address),
                task,
                _system.CPU.UCodeMemory.GetBank(task),
                Conversion.ToOctal(_system.CPU.R[6]));

            return _keyData[address - 0xfe60];

        }

        /// <summary>
        /// Writes a word to the specified address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public void Load(int address, ushort data, TaskType task, bool extendedMemory)
        {

            // The registers are write-only as far as I've been able to ascertain.
            Log.Write(LogType.Verbose, LogComponent.Organ, "Unexpected organ write to {0} ({1}) by task {2} (bank {3})",
                Conversion.ToOctal(address),
                Conversion.ToOctal(data),
                task,
                _system.CPU.UCodeMemory.GetBank(task));
        }

        /// <summary>
        /// Specifies the range (or ranges) of addresses decoded by this device.
        /// </summary>
        public MemoryRange[] Addresses
        {
            get { return _addresses; }
        }


        /// <summary>
        /// From: http://bitsavers.org/pdf/xerox/alto/memos_1975/Reserved_Alto_Memory_Locations_Jan75.pdf
        /// 
        /// #177140 - #177157: Organ Keyboard (Organ Hardware - Kaehler)
        /// </summary>
        private readonly MemoryRange[] _addresses =
        {
            new MemoryRange(0xfe60, 0xfe6f),
        };

        private ushort[] _keyData = new ushort[16];

        private AltoSystem _system;
    }

}