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
using System.Runtime.CompilerServices;

namespace Contralto.Memory
{
    /// <summary>
    /// Implements the Alto's main memory, up to 4 banks of 64KW in 16-bit words.
    /// Provides implementation of the IIXM's memory mapping hardware.
    /// </summary>
    public class Memory : IMemoryMappedDevice
    {
        public Memory(Configuration configuration)
        {
            // Set up handled addresses based on the system type.
            if (configuration.SystemType == SystemType.AltoI)
            {
                _addresses = new MemoryRange[]
                {
                    new MemoryRange(0, _memTop),                                     // Main bank of RAM to 176777; IO page above this.                    
                };
            }
            else
            {
                _addresses = new MemoryRange[]
                {
                    new MemoryRange(0, _memTop),                                     // Main bank of RAM to 176777; IO page above this.
                    new MemoryRange(_xmBanksStart, (ushort)(_xmBanksStart + 16)),    // Memory bank registers
                };
            }

            Reset();
        }

        /// <summary>
        /// The top address of main memory (above which lies the I/O space)
        /// </summary>
        public static ushort RamTop
        {
            get { return _memTop; }
        }

        /// <summary>
        /// Full reset, clears all memory.
        /// </summary>
        public void Reset()
        {
            // 4 64K banks, regardless of system type.  (Alto Is just won't use the extra memory.)
            _mem = new ushort[0x40000];
            _xmBanks = new ushort[16];
            _xmBanksAlternate = new int[16];
            _xmBanksNormal = new int[16];
        }

        /// <summary>
        /// Soft reset, clears XM bank registers.
        /// </summary>
        public void SoftReset()
        {
            _xmBanks = new ushort[16];
            _xmBanksAlternate = new int[16];
            _xmBanksNormal = new int[16];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Read(int address, TaskType task, bool extendedMemory)
        {
            // Check for XM registers; this occurs regardless of XM flag since it's in the I/O page.
            if (address >= _xmBanksStart && address < _xmBanksStart + 16)
            {
                // NB: While not specified in documentation, some code (IFS in particular) relies on the fact that
                // the upper 12 bits of the bank registers are all 1s.
                return (ushort)(0xfff0 | _xmBanks[address - _xmBanksStart]);
            }
            else
            {
                return _mem[address + 0x10000 * GetBankNumber(task, extendedMemory)]; 
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Load(int address, ushort data, TaskType task, bool extendedMemory)
        {
            // Check for XM registers; this occurs regardless of XM flag since it's in the I/O page.
            if (address >= _xmBanksStart && address < _xmBanksStart + 16)
            {
                _xmBanks[address - _xmBanksStart] = data;

                // Precalc bank numbers to speed memory accesses that use them
                _xmBanksAlternate[address - _xmBanksStart] = data & 0x3;
                _xmBanksNormal[address - _xmBanksStart] = (data & 0xc) >> 2;

                Log.Write(LogComponent.Memory, "XM register for task {0} set to bank {1} (normal), {2} (xm)",
                    (TaskType)(address - _xmBanksStart),
                    (data & 0xc) >> 2,
                    (data & 0x3));
            }
            else
            {
                _mem[address + 0x10000 * GetBankNumber(task, extendedMemory)] = data;
            }
        }

        public MemoryRange[] Addresses
        {
            get { return _addresses; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetBankNumber(TaskType task, bool extendedMemory)
        {
            return extendedMemory ?
                _xmBanksAlternate[(int)task] : _xmBanksNormal[(int)task];
        }

        private readonly MemoryRange[] _addresses;

        private static readonly ushort _memTop = 0xfdff;         // 176777
        private static readonly ushort _xmBanksStart = 0xffe0;   // 177740

        private ushort[] _mem;

        private ushort[] _xmBanks;        
        private int[] _xmBanksAlternate;
        private int[] _xmBanksNormal;
    }
}
