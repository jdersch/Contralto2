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
    public enum ShifterOp
    {        
        None,
        ShiftLeft,
        ShiftRight,
        RotateLeft,
    }

    public enum ShifterModifier
    {
        None,
        Magic,
        DNS,
    }

    // NOTE: FOR NOVA (NOVEL) SHIFTS (from aug '76 manual):
    // The emulator has two additional bits of state, the SKIP and CARRY flip flops.CARRY is identical
    // to the Nova carry bit, and is set or cleared as appropriate when the DNS+- (do Nova shifts)
    // function is executed.  DNS also addresses R from (R[3 - 4] XOR 3), and sets the SKIP flip flop if 
    // appropriate.The PC is incremented by 1 at the beginning of the next emulated instruction if
    // SKIP is set, using ALUF DB.  IR<- clears SKIP.
    public static class Shifter
    {
        static Shifter()
        {
            Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reset()
        {
            _op = ShifterOp.None;            
            _output = 0;
            _modifier = ShifterModifier.None;
            _dnsCarry = 0;
        }

        /// <summary>
        /// Returns the result of the last Shifter operation (via DoOperation).
        /// </summary>
        public static ushort Output
        {
            get { return _output; }
            set { _output = value; }
        }

        public static ShifterOp Op
        {
            get { return _op; }
        }

        /// <summary>
        /// Returns the last DNS-style Carry bit from the last operation (via DoOperation),
        /// or sets the carry-in for the next DNS-style shift.
        /// </summary>
        public static int DNSCarry
        {
            get { return _dnsCarry; }
            set
            {
                // Sanity check
                if (value != 0 && value != 1)
                {
                    throw new InvalidOperationException("Invalid DNSCarry value.");
                }
                _dnsCarry = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetOperation(ShifterOp op)
        {
            _op = op;            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetModifier(ShifterModifier mod)
        {
            _modifier = mod;
        }

        /// <summary>
        /// Does the last specified operation to the specified inputs; the result
        /// can be read from Output.
        /// </summary>
        /// <param name="input">Normal input to be shifted</param>
        /// <param name="t">CPU t register, used for MAGIC shifts only</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort DoOperation(ushort input, ushort t)
        {           
            switch(_op)
            {               
                case ShifterOp.None:
                    _output = input;
                    break;

                case ShifterOp.ShiftLeft:
                    _output = (ushort)(input << 1);                   

                    switch (_modifier)
                    {
                        case ShifterModifier.Magic:
                            // "MAGIC places the high order bit of T into the low order bit of the
                            // shifter output on left shifts..."
                            _output |= (ushort)((t & 0x8000) >> 15);
                            break;

                        case ShifterModifier.DNS:
                            //
                            // "Rotate the 17 input bits left by one bit.  This has the effect of rotating
                            // bit 0 left into the carry position and the carry bit into bit 15."
                            //

                            // Put input carry into bit 15.
                            _output |= (ushort)(_dnsCarry);

                            // update carry
                            _dnsCarry = ((input & 0x8000) >> 15);
                            break;
                    }
                    
                    break;

                case ShifterOp.ShiftRight:
                    _output = (ushort)(input >> 1);                    

                    switch (_modifier)
                    {
                        case ShifterModifier.Magic:                    
                            _output |= (ushort)((t & 0x1) << 15);
                            break;

                        case ShifterModifier.DNS:
                            //
                            // "Rotate the 17 bits right by one bit.  Bit 15 is rotated into the carry position
                            // and the carry bit into bit 0."
                            //

                            // Put input carry into bit 0.
                            _output |= (ushort)(_dnsCarry << 15);

                            // update carry
                            _dnsCarry = input & 0x1;
                            break;
                    }
                    break;

                case ShifterOp.RotateLeft:                                        
                    //
                    // "Swap the 8-bit halves of the 16-bit result.  The carry is not affected."
                    // NOTE:  The hardware reference (Section 2) seems to indicate that L LCY 8 is modified by MAGIC and/or DNS,
                    // but this does not appear to actually be the case.  Nothing in the documentation elsewhere, the microcode,
                    // or the schematics indicates that L LCY 8 ever does anything other than a simple swap.
                    //
                    _output = (ushort)(((input & 0xff00) >> 8) | ((input & 0x00ff) << 8));                                        
                    break;                   

                default:
                    throw new InvalidOperationException(String.Format("Unhandled shift operation {0}", _op));
            }            

            return _output;
        }

        private static ShifterOp _op;
        private static ushort _output;        
        private static ShifterModifier _modifier;
        private static int _dnsCarry;
    }
}
