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

namespace Contralto.CPU
{
    public partial class AltoCPU
    {
        /// <summary>
        /// OrbitTask provides the implementation of the Orbit (printer rasterizer) controller
        /// specific functions.
        /// </summary>
        private sealed class OrbitTask : Task
        {
            public OrbitTask(AltoCPU cpu) : base(cpu)
            {
                _taskType = TaskType.Orbit;
                _wakeup = false;

                // The Orbit task is RAM-related.
                _ramTask = true;
            }

            protected override void ExecuteBlock()
            {
                _cpu._system.OrbitController.Stop();
            }            

            protected override ushort GetBusSource(MicroInstruction instruction)
            {
                //
                // The Orbit task is wired to be a RAM-enabled task so it can use
                // S registers.
                // This code is stolen from the Emulator task; we should refactor this...
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

            protected override void ExecuteSpecialFunction1Early(MicroInstruction instruction)
            {
                OrbitF1 of1 = (OrbitF1)instruction.F1;
                switch (of1)
                { 

                    case OrbitF1.OrbitDeltaWC:
                        _busData &= _cpu._system.OrbitController.GetDeltaWC();
                        break;

                    case OrbitF1.OrbitDBCWidthRead:
                        _busData &= _cpu._system.OrbitController.GetDBCWidth();
                        break;

                    case OrbitF1.OrbitOutputData:
                        _busData &= _cpu._system.OrbitController.GetOutputDataAlto();
                        break;

                    case OrbitF1.OrbitStatus:
                        _busData &= _cpu._system.OrbitController.GetOrbitStatus();

                        // branch:
                        // "OrbitStatus sets NEXT[7] of IACS os *not* on, i.e. if Orbit is
                        //  not in a character segment."
                        //
                        if (!_cpu._system.OrbitController.IACS)
                        {
                            _nextModifier |= 0x4;
                        }
                        break;
                }
            }

            protected override void ExecuteSpecialFunction2(MicroInstruction instruction)
            {
                OrbitF2 of2 = (OrbitF2)instruction.F2;
                switch (of2)
                {
                    case OrbitF2.OrbitDBCWidthSet:
                        _cpu._system.OrbitController.SetDBCWidth(_busData);
                        break;

                    case OrbitF2.OrbitXY:
                        _cpu._system.OrbitController.SetXY(_busData);
                        break;

                    case OrbitF2.OrbitHeight:
                        _cpu._system.OrbitController.SetHeight(_busData);

                        // branch:
                        // "OrbitHeight sets NEXT[7] if the refresh timer has expired, i.e.
                        //  if the image buffer needs refreshing."
                        //
                        if (_cpu._system.OrbitController.RefreshTimerExpired)
                        {                            
                            _nextModifier |= 0x4;
                        }
                        break;

                    case OrbitF2.OrbitFontData:
                        _cpu._system.OrbitController.WriteFontData(_busData);
                        break;

                    case OrbitF2.OrbitInk:
                        _cpu._system.OrbitController.WriteInkData(_busData);
                        break;

                    case OrbitF2.OrbitControl:
                        _cpu._system.OrbitController.Control(_busData);
                        break;

                    case OrbitF2.OrbitROSCommand:
                        _cpu._system.OrbitController.SendROSCommand(_busData);
                        break;

                    default:
                        throw new InvalidOperationException(String.Format("Unhandled orbit F2 {0}.", of2));
                }
            }
        }
    }
}
