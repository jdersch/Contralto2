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

using System.Runtime.CompilerServices;

namespace Contralto.CPU
{
    //
    // This implements the stripped-down version of the 74181 ALU 
    // that the Alto exposes to the microcode, and nothing more.
    //
    static class ALU
    {
        static ALU()
        {
            Reset();
        }

        public static void Reset()
        {
            _carry = 0;
        }

        public static int Carry
        {
            get { return _carry; }
            set { _carry = value; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Execute(AluFunction fn, ushort bus, ushort t, int skip)
        {
            int r;
            switch (fn)
            {
                case AluFunction.Bus:
                    _carry = 0;     // M = 1
                    r = bus;
                    break;

                case AluFunction.T:
                    _carry = 0;     // M = 1
                    r = t;
                    break;

                case AluFunction.BusOrT:
                    _carry = 0;     // M = 1
                    r = (bus | t);
                    break;

                case AluFunction.BusAndT:
                case AluFunction.AluBusAndT:
                    _carry = 0;     // M = 1
                    r = (bus & t);
                    break;

                case AluFunction.BusXorT:
                    _carry = 0;     // M = 1
                    r = (bus ^ t);
                    break;

                case AluFunction.BusPlus1:
                    r = bus + 1;
                    _carry = (r > 0xffff) ? 1 : 0;
                    break;

                case AluFunction.BusMinus1:
                    r = bus - 1;

                    // Just for clarification; the datasheet specifies:
                    // "Because subtraction is actually performed by complementary
                    //  addition (1s complement), a carry out means borrow; thus,
                    //  a carry is generated when there is no underflow and no carry
                    //  is generated when there is underflow."
                    _carry = (r < 0) ? 0 : 1;
                    break;

                case AluFunction.BusPlusT:
                    r = bus + t;
                    _carry = (r > 0xffff) ? 1 : 0;
                    break;

                case AluFunction.BusMinusT:
                    r = bus - t;
                    _carry = (r < 0) ? 0 : 1;
                    break;

                case AluFunction.BusMinusTMinus1:
                    r = bus - t - 1;
                    _carry = (r < 0) ? 0 : 1;
                    break;

                case AluFunction.BusPlusTPlus1:
                    r = bus + t + 1;
                    _carry = (r > 0xffff) ? 1 : 0;
                    break;

                case AluFunction.BusPlusSkip:
                    r = bus + skip;
                    _carry = (r > 0xffff) ? 1 : 0;
                    break;

                case AluFunction.BusAndNotT:
                    r = bus & (~t);
                    _carry = 0;
                    break;

                default:
                    throw new InvalidOperationException(String.Format("Unhandled ALU function {0}", fn));
            }     
       
            return (ushort)r;
        }

        private static int _carry;
    }
}
