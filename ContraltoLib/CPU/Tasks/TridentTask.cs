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

using Contralto.IO;

namespace Contralto.CPU
{
    public partial class AltoCPU
    {
        /// <summary>
        /// TridentTask implements the microcode special functions for the Trident
        /// disk controller (for both the read and write tasks).
        /// </summary>
        private sealed class TridentTask : Task
        {
            public TridentTask(AltoCPU cpu, bool input) : base(cpu)
            {
                _taskType = input ? TaskType.TridentInput : TaskType.TridentOutput;
                _wakeup = false;

                _tridentController = cpu._system.TridentController;

                // Both Trident tasks are RAM-related
                _ramTask = true;
            }

            public override void SoftReset()
            {
                //
                // Stop the controller.
                //
                _tridentController.Stop();

                base.SoftReset();
            }

            protected override ushort GetBusSource(MicroInstruction instruction)
            {
                //
                // The Trident tasks are wired to be RAM-enabled tasks so they can use
                // S registers.
                // This code is stolen from the Emulator task; we should REALLY refactor this
                // since both this and the Orbit Task need it.
                //
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

            protected override void ExecuteSpecialFunction2PostBusSource(MicroInstruction instruction)
            {
                TridentF2 tf2 = (TridentF2)instruction.F2;

                switch (tf2)
                {
                    case TridentF2.ReadKDTA:
                        //
                        // <-KDTA is actually MD<- (SF 6), repurposed to gate disk data onto the bus
                        // iff BS is None.  Otherwise it behaves like a normal MD<-.  We'll let
                        // the normal Task implementation handle the actual MD<- operation.
                        //
                        if (instruction.BS == BusSource.None)
                        {
                            // _busData at this point should be 0xffff.  We could technically
                            // just directly assign the bits...
                            _busData &= _tridentController.KDTA;
                        }
                        break;

                    case TridentF2.STATUS:
                        _busData &= _tridentController.STATUS;
                        break;

                    case TridentF2.EMPTY:
                        _tridentController.WaitForEmpty();
                        break;

                }
            }

            protected override void ExecuteSpecialFunction2(MicroInstruction instruction)
            {
                TridentF2 tf2 = (TridentF2)instruction.F2;

                switch (tf2)
                {
                    case TridentF2.KTAG:
                        _tridentController.TagInstruction(_busData);
                        break;

                    case TridentF2.WriteKDTA:
                        _tridentController.KDTA = _busData;
                        break;

                    case TridentF2.WAIT:
                    case TridentF2.WAIT2:
                        // Identical to BLOCK
                        this.BlockTask();
                        break;

                    case TridentF2.RESET:
                        _tridentController.ControllerReset();
                        break;

                    case TridentF2.STATUS:
                    case TridentF2.EMPTY:
                        // Handled in PostBusSource override.
                        break;

                    default:
                        throw new InvalidOperationException(String.Format("Unhandled trident special function 2 {0}", tf2));
                }
            }

            private TridentController _tridentController;

        }        
    }
}
