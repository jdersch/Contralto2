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

using Contralto.Logging;
using Contralto.Scripting;
using System.Runtime.CompilerServices;

namespace Contralto.CPU
{
    public partial class AltoCPU
    {
        /// <summary>
        /// EmulatorTask provides emulator (NOVA instruction set) specific operations.
        /// </summary>
        private sealed class EmulatorTask : Task
        {
            public EmulatorTask(AltoCPU cpu) : base(cpu)
            {
                _taskType = TaskType.Emulator;

                // The Wakeup signal is always true for the Emulator task.
                _wakeup = true;

                // The Emulator is a RAM-related task.
                _ramTask = true;
            }

            public override void Reset()
            {
                base.Reset();
                _wakeup = true;
            }

            public override void BlockTask()
            {
                throw new InvalidOperationException("The emulator task cannot be blocked.");
            }

            public override void WakeupTask()
            {
                throw new InvalidOperationException("The emulator task is always in wakeup state.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override ushort GetBusSource(MicroInstruction instruction)
            {
                EmulatorBusSource ebs = (EmulatorBusSource)instruction.BS;

                switch (ebs)
                {
                    case EmulatorBusSource.ReadSLocation:
                        if (instruction.RSELECT != 0)
                        {
                            return _cpu._s[_rb][instruction.RSELECT];
                        }
                        else
                        {
                            // "...when reading data from the S registers onto the processor bus,
                            //  the RSELECT value 0 causes the current value of the M register to
                            //  appear on the bus..."
                            return _cpu._m;
                        }

                    case EmulatorBusSource.LoadSLocation:
                        // "When an S register is being loaded from M, the processor bus receives an
                        // undefined value rather than being set to zero."
                        _loadS = true;
                        return 0xffff;       // Technically this is an "undefined value," we're defining it as -1.

                    default:
                        throw new InvalidOperationException(String.Format("Unhandled bus source {0}", instruction.BS));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void ExecuteSpecialFunction1Early(MicroInstruction instruction)
            {
                EmulatorF1 ef1 = (EmulatorF1)instruction.F1;
                switch (ef1)
                {
                    case EmulatorF1.RSNF:
                        //   
                        // Early:
                        // "...decoded by the Ethernet interface, which gates the host address wired on the
                        // backplane onto BUS[8-15].  BUS[0-7] is not driven and will therefore be -1.  If
                        // no Ethernet interface is present, BUS will be -1.
                        //
                        _busData &= (ushort)((0xff00 | _cpu._system.EthernetController.Address));
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void ExecuteSpecialFunction1(MicroInstruction instruction)
            {
                EmulatorF1 ef1 = (EmulatorF1)instruction.F1;
                switch (ef1)
                {
                    case EmulatorF1.LoadRMR:
                        //
                        // "The emulator F1 RMR<- causes the reset mode register to be loaded from the processor bus.  The 16 bits of the
                        // processor bus correspond to the 16 Alto tasks in the following way: the low order bit of the processor
                        // bus specifies the initial mode of task 0, the lowest priority task (emulator), and the high-order bit of the
                        // bus specifies the initial mode of task 15, the highest priority task(recall that task i starts at location i; the
                        // reset mode register determines only which microinstruction bank will be used at the outset). A task will
                        // commence in ROM0 if its associated bit in the reset mode register contains the value 1; otherwise it will
                        // start in RAM0.  Upon initial power-up of the Alto, and after each reset operation, the reset mode register
                        // is automatically set to all ones, corresponding to starting all tasks in ROM0."
                        //
                        _cpu._rmr = _busData;
                        break;

                    case EmulatorF1.RSNF:
                        // Handled in the Early handler.
                        break;

                    case EmulatorF1.STARTF:
                        // Dispatch function to Ethernet I/O based on contents of AC0.
                        if ((_busData & 0x8000) != 0)
                        {
                            
                            // Since this is a soft reset, we don't want MPC to be taken from the NEXT
                            // field at the end of the cycle, setting this flag causes the main Task
                            // implementation to skip updating _mpc at the end of this instruction.
                            _softReset = true;
                        }
                        else if(_busData != 0)
                        {
                            //
                            // Dispatch to the appropriate device.
                            //
                            switch(_busData)
                            {
                                case 0x01:
                                case 0x02:
                                case 0x03:
                                    // Ethernet
                                    _cpu._system.EthernetController.STARTF(_busData);
                                    break;

                                case 0x04:
                                    // Orbit
                                    _cpu._system.OrbitController.STARTF(_busData);
                                    break;

                                case 0x10:
                                case 0x20:
                                    // Trident start
                                    _cpu._system.TridentController.STARTF(_busData);
                                    break;

                                    //
                                    // The following are not actual Alto STARTF functions,
                                    // these are used to allow writing Alto programs that can
                                    // alter behavior of the emulator.  At the moment, these
                                    // are all related to scripting, and are only enabled
                                    // when a script is running.
                                    //
                                case 0x2000:
                                    //
                                    // Unpause script.
                                    // 
                                    if (ScriptManager.IsPlaying)
                                    {
                                        ScriptManager.CompleteWait();
                                    }
                                    break;

                                case 0x4000:
                                    //
                                    // Emulator exit, commit disks.
                                    //
                                    if (ScriptManager.IsPlaying)
                                    {
                                        throw new ShutdownException(true);
                                    }
                                    break;

                                default:
                                    Log.Write(Logging.LogType.Warning, Logging.LogComponent.EmulatorTask, "STARTF for unknown device (code {0})",
                                        Conversion.ToOctal(_busData));
                                    break;
                            }
                        }
                        break;

                    case EmulatorF1.SWMODE:
                        _swMode = true;
                        break;

                    case EmulatorF1.RDRAM:
                        // TODO: move RDRAM, WRTRAM and S-register BS stuff into the main Task implementation,
                        // guarded by a check of _ramTask.
                        _rdRam = true;
                        break;

                    case EmulatorF1.WRTRAM:
                        _wrtRam = true;
                        break;

                    case EmulatorF1.LoadESRB:
                        _rb = (ushort)((_busData & 0xe) >> 1);

                        if (_rb != 0 && _systemType != SystemType.ThreeKRam)
                        {
                            // Force bank 0 for machines with only 1K RAM.
                            _rb = 0;
                        }
                        break;

                    default:
                        throw new InvalidOperationException(String.Format("Unhandled emulator F1 {0}.", ef1));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void ExecuteSpecialFunction2Early(MicroInstruction instruction)
            {
                EmulatorF2 ef2 = (EmulatorF2)instruction.F2;
                switch (ef2)
                {
                    case EmulatorF2.ACSOURCE:
                        // Early: modify R select field:
                        // "...it replaces the two-low order bits of the R select field with
                        // the complement of the SrcAC field of IR, (IR[1-2] XOR 3), allowing the emulator
                        // to address its accumulators (which are assigned to R0-R3)."
                        _rSelect = (_rSelect & 0xfffc) | ((((uint)_cpu._ir & 0x6000) >> 13) ^ 3);
                        break;

                    case EmulatorF2.ACDEST:
                        // "...causes (IR[3-4] XOR 3) to be used as the low-order two bits of the RSELECT field.
                        // This address the accumulators from the destination field of the instruction.  The selected
                        // register may be loaded or read."
                    case EmulatorF2.LoadDNS:
                        //
                        // "...DNS also addresses R from (3-IR[3 - 4])..."
                        //
                        _rSelect = (_rSelect & 0xfffc) | ((((uint)_cpu._ir & 0x1800) >> 11) ^ 3);                        
                        break;

                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void ExecuteSpecialFunction2(MicroInstruction instruction)
            {
                EmulatorF2 ef2 = (EmulatorF2)instruction.F2;
                switch (ef2)
                {
                    case EmulatorF2.LoadIR:
                        // Load IR from the bus                        
                        _cpu._ir = _busData;

                        // "IR<- also merges bus bits 0, 5, 6 and 7 into NEXT[6-9] which does a first level
                        // instruction dispatch."                                                
                        _nextModifier |= (ushort)(((_busData & 0x8000) >> 12) | ((_busData & 0x0700) >> 8));

                        // "IR<- clears SKIP"
                        _skip = 0;
                        break;

                    case EmulatorF2.IDISP:
                        // "The IDISP function (F2=15B) does a 16 way dispatch under control of a PROM and a
                        // multiplexer.  The values are tabulated below:
                        //   Conditions             ORed onto NEXT          Comment
                        //
                        //   if IR[0] = 1           3-IR[8-9]               complement of SH field of IR
                        //   elseif IR[1-2] = 0     IR[3-4]                 JMP, JSR, ISZ, DSZ              ; dispatch selects register
                        //   elseif IR[1-2] = 1     4                       LDA
                        //   elseif IR[1-2] = 2     5                       STA
                        //   elseif IR[4-7] = 0     1                       
                        //   elseif IR[4-7] = 1     0
                        //   elseif IR[4-7] = 6     16B                     CONVERT
                        //   elseif IR[4-7] = 16B   6
                        //   else                   IR[4-7]
                        // NB: as always, Xerox labels bits in the opposite order from modern convention;
                        // (bit 0 is the msb...)     
                        //
                        // NOTE: The above table is accurate and functions correctly; using the PROM is faster.
                        //                                         
                        if ((_cpu._ir & 0x8000) != 0)
                        {
                            _nextModifier |= (ushort)(3 - ((_cpu._ir & 0xc0) >> 6));
                        }
                        else
                        {
                            _nextModifier |= _cpu.ControlROM.ACSourceROM[((_cpu._ir & 0x7f00) >> 8) + 0x80];
                        }                                              
                        break;

                    case EmulatorF2.ACSOURCE:
                        // Late:
                        // "...a dispatch is performed:
                        //   Conditions             ORed onto NEXT          Comment
                        //
                        //   if IR[0] = 1           3-IR[8-9]               complement of SH field of IR
                        //   if IR[1-2] != 3        IR[5]                   the Indirect bit of R
                        //   if IR[3-7] = 0         2                       CYCLE
                        //   if IR[3-7] = 1         5                       RAMTRAP
                        //   if IR[3-7] = 2         3                       NOPAR -- parameterless opcode group
                        //   if IR[3-7] = 3         6                       RAMTRAP
                        //   if IR[3-7] = 4         7                       RAMTRAP
                        //   if IR[3-7] = 11B       4                       JSRII
                        //   if IR[3-7] = 12B       4                       JSRIS
                        //   if IR[3-7] = 16B       1                       CONVERT
                        //   if IR[3-7] = 37B       17B                     ROMTRAP -- used by Swat, the debugger
                        //   else                   16B                     ROMTRAP

                        //                         
                        // NOTE: The above table is accurate and functions correctly; using the PROM is faster.
                        //    
                        if ((_cpu._ir & 0x8000) != 0)
                        {
                            // 3-IR[8-9] (shift field of arithmetic instruction)
                            _nextModifier |= (ushort)(3 - ((_cpu._ir & 0xc0) >> 6));
                        }
                        else
                        {                            
                            // Use the PROM.                            
                            _nextModifier |= _cpu._controlROM.ACSourceROM[((_cpu._ir & 0x7f00) >> 8)];                                                       
                        }
                                           
                        break;

                    case EmulatorF2.ACDEST:
                        // Handled in early handler, nothing to do here.
                        break;

                    case EmulatorF2.BUSODD:
                        // "...merges BUS[15] into NEXT[9]."
                        _nextModifier |= (ushort)(_busData & 0x1);
                        break;

                    case EmulatorF2.MAGIC:
                        Shifter.SetModifier(ShifterModifier.Magic);
                        break;
                        
                    case EmulatorF2.LoadDNS:
                        // DNS<- does the following:
                        // - modifies the normal shift operations to perform Nova-style shifts (done here)
                        // - addresses R from 3-IR[3-4] (destination AC)  (see Early LoadDNS handler)
                        // - stores into R unless IR[12] is set (done here) 
                        //   [NOTE: This overrides a LoadR BS field if present -- that is, if IR[12] is set and
                        //    BS=LoadR, no load into R will take place.  Note also that the standard
                        //    microcode apparently always specifies a LoadR BS for LoadDNS microinstructions.  Need to
                        //    look at the schematics more closely to see if this is required or just a convention
                        //    of the PARC microassembler.]
                        // - calculates Nova-style CARRY bit (done here)
                        // - sets the SKIP and CARRY flip-flops appropriately (see Late LoadDNS handler)
                        int carry = 0;                                             

                        // Also indicates modifying CARRY
                        _loadR = (_cpu._ir & 0x0008) == 0;
                        
                        // At this point the ALU has already done its operation but the shifter has not yet run.
                        // We need to set the CARRY bit that will be passed through the shifter appropriately.
                        
                        // Select carry input value based on carry control
                        switch((_cpu._ir & 0x30) >> 4)
                        {
                            case 0x0:
                                // Nothing; CARRY unaffected.
                                carry = _carry;
                                break;

                            case 0x1:
                                carry = 0;  // Z
                                break;

                            case 0x2:
                                carry = 1;  // O
                                break;

                            case 0x3:
                                carry = (~_carry) & 0x1;  // C
                                break;
                        }

                        // Now modify the result based on the current ALU result
                        switch ((_cpu._ir & 0x700) >> 8)
                        {
                            case 0x0:
                            case 0x2:
                            case 0x7:
                                // COM, MOV, AND - Carry unaffected
                                break;

                            case 0x1:                                                                
                            case 0x3:
                            case 0x4:
                            case 0x5:
                            case 0x6:
                                // NEG, INC, ADC, SUB, ADD - invert the carry bit
                                if (_cpu._aluC0 != 0)
                                {
                                    carry = (~carry) & 0x1;
                                }
                                break;                                
                        }

                        // Tell the Shifter to do a Nova-style shift with the
                        // given carry bit.
                        Shifter.SetModifier(ShifterModifier.DNS);
                        Shifter.DNSCarry = carry;

                        break; 

                    default:
                        throw new InvalidOperationException(String.Format("Unhandled emulator F2 {0}.", ef2));                        
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void ExecuteSpecialFunction2Late(MicroInstruction instruction)
            {
                EmulatorF2 ef2 = (EmulatorF2)instruction.F2;
                switch (ef2)
                {
                    case EmulatorF2.LoadDNS:
                        //
                        // Set SKIP and CARRY flip-flops based on the final result of the operation after having
                        // passed through the shifter.
                        //
                        ushort result = Shifter.Output;
                        int carry = Shifter.DNSCarry;
                        switch (_cpu._ir & 0x7)
                        {
                            case 0:
                                // None, SKIP is reset
                                _skip = 0;
                                break;

                            case 1:     // SKP
                                // Always skip
                                _skip = 1;
                                break;

                            case 2:     // SZC
                                // Skip if carry result is zero
                                _skip = (carry == 0) ? 1 : 0;
                                break;

                            case 3:     // SNC
                                // Skip if carry result is nonzero
                                _skip = carry;
                                break;

                            case 4:     // SZR
                                _skip = (result == 0) ? 1 : 0;
                                break;

                            case 5:     // SNR
                                _skip = (result != 0) ? 1 : 0;
                                break;

                            case 6:     // SEZ
                                _skip = (result == 0 || carry == 0) ? 1 : 0;
                                break;

                            case 7:     // SBN
                                _skip = (result != 0 && carry != 0) ? 1 : 0;
                                break;
                        }

                        if (_loadR)
                        {
                            // Write carry flag back.
                            _carry = carry;
                        }

                        break;
                }
            

            }

            // From Section 3, Pg. 31:
            // "The emulator has two additional bits of state, the SKIP and CARRY flip flops. CARRY is distinct from the
            // microprocessor’s ALUC0 bit, tested by the ALUCY function.  CARRY is set or cleared as a function of IR and
            // many other things(see section 3.1) when the DNS<-(do novel shifts, F2= 12B) function is executed.  In
            // particular, if IR[12] is true, CARRY will not change.  DNS also addresses R from (3-IR[3 - 4]), causes a store
            // into R unless IR[12] is set, and sets the SKIP flip flop if appropriate(see section 3.1).  The emulator
            // microcode increments PC by 1 at the beginning of the next emulated instruction if SKIP is set, using
            // BUS+SKIP(ALUF= 13B).  IR<- clears SKIP."
            //
            // NB: _skip is in the encapsulating AltoCPU class to make it easier to reference since the ALU needs to know about it.
            private int _carry;        
        }
    }
}
