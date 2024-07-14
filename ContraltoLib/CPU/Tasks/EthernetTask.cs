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

using Contralto.IO;
using Contralto.Logging;

namespace Contralto.CPU
{
    public partial class AltoCPU
    {
        /// <summary>
        /// EthernetTask implements Ethernet-specific task functions
        /// </summary>
        private sealed class EthernetTask : Task
        {
            public EthernetTask(AltoCPU cpu) : base(cpu)
            {
                _taskType = TaskType.Ethernet;                
                _wakeup = false;

                _ethernetController = _cpu._system.EthernetController;
            }

            protected override InstructionCompletion ExecuteInstruction(MicroInstruction instruction)
            {                
                // The Ethernet task only remains awake if there are pending data wakeups                
                if (_ethernetController.CountdownWakeup)
                {
                    //
                    // The resulting [Countdown] wakeup is cleared when the Ether task next runs.                        
                    _ethernetController.CountdownWakeup = false;
                    _wakeup = false;
                }                           

                return base.ExecuteInstruction(instruction);
            }            

            protected override ushort GetBusSource(MicroInstruction instruction)
            {
                EthernetBusSource ebs = (EthernetBusSource)instruction.BS;

                switch(ebs)
                {
                    case EthernetBusSource.EIDFCT:
                        // Input Data Function. Gates the contents of the FIFO to BUS[0-15], and
                        // increments the read pointer at the end of the cycle.
                        return _ethernetController.ReadInputFifo(false /* increment read pointer */);

                    default:
                        throw new NotImplementedException(String.Format("Unimplemented Ethernet BS {0}", ebs));
                }
            }

            protected override void ExecuteSpecialFunction1Early(MicroInstruction instruction)
            {
                EthernetF1 ef1 = (EthernetF1)instruction.F1;
                switch (ef1)
                {
                    case EthernetF1.EILFCT:
                        // Early: Input Look Function. Gates the contents of the FIFO to BUS[0-15] but does
                        // not increment the read pointer.
                        _busData &= _ethernetController.ReadInputFifo(true /* do not increment read pointer */);
                        break;
                }
            }

            protected override void ExecuteSpecialFunction1(MicroInstruction instruction)
            {
                EthernetF1 ef1 = (EthernetF1)instruction.F1;
                switch(ef1)
                {
                    case EthernetF1.EILFCT:
                        // Nothing; handled in Early handler.
                        break;

                    case EthernetF1.EPFCT:
                        // Post Function. Gates interface status to BUS[8-15]. Resets the interface at
                        // the end of the cycle and removes wakeup for this task.
                        _busData &= _ethernetController.Status;
                        _ethernetController.ResetInterface();
                        _wakeup = false;
                        Log.Write(LogComponent.EthernetController, "EPFCT: Status {0}, bus now {1}",
                            Conversion.ToOctal(_ethernetController.Status),
                            Conversion.ToOctal(_busData));
                        break;

                    case EthernetF1.EWFCT:
                        // Countdown Wakeup Function. Sets a flip flop in the interface that will
                        // cause a wakeup to the Ether task on the next tick of SWAKMRT. This
                        // function must be issued in the instruction after a TASK. The resulting
                        // wakeup is cleared when the Ether task next runs.      
                        Log.Write(LogComponent.EthernetController, "Enabling countdown wakeups.");
                        _ethernetController.CountdownWakeup = true;
                        break;                    

                    default:
                        throw new NotImplementedException(String.Format("Unimplemented Ethernet F1 {0}", ef1));

                }
            }

            protected override void ExecuteSpecialFunction2(MicroInstruction instruction)
            {
                EthernetF2 ef2 = (EthernetF2)instruction.F2;
                switch (ef2)
                {
                    case EthernetF2.EODFCT:
                        // Output Data Function. Loads the FIFO from BUS[0-15], then increments the
                        // write pointer at the end of the cycle.
                        _ethernetController.WriteOutputFifo(_busData);
                        break;

                    case EthernetF2.EOSFCT:
                        // Output Start Function. Sets the OBusy flip flop in the interface, starting
                        // data wakeups to fill the FIFO for output. When the FIFO is full, or EEFct has
                        // been issued, the interface will wait for silence on the Ether and begin
                        // transmitting.
                        _ethernetController.StartOutput();
                        break;

                    case EthernetF2.ERBFCT:
                        // Reset Branch Function. This command dispatch function merges the ICMD
                        // and OCMD flip flops, into NEXT[6-7]. These flip flops are the means of
                        // communication between the emulator task and the Ethernet task. The
                        // emulator task sets them from BUS[14-15] with the STARTF function, causing
                        // the Ethernet task to wakeup, dispatch on them and then reset them with
                        // EPFCT.
                        Log.Write(LogComponent.EthernetController, "EBRBFCT: SIO is {0}.", _ethernetController.IOCMD);
                        _nextModifier |= (ushort)((_ethernetController.IOCMD << 2));
                        break;

                    case EthernetF2.EEFCT:
                        // End of transmission Function. This function is issued when all of the main
                        // memory output buffer has been transferred to the FIFO. EEFCT disables
                        // further data wakeups.
                        _ethernetController.EndTransmission();
                        break;

                    case EthernetF2.EBFCT:
                        // Branch Function. ORs a one into NEXT[7] if an input data late is detected,
                        // or an SIO with AC0[14:15] non-zero is issued, or if the transmitter or receiver
                        // goes done. ORs a one into NEXT[6] if a collision is detected.
                        
                        if (_ethernetController.DataLate || 
                            _ethernetController.IOCMD != 0 ||
                            _ethernetController.OperationDone)
                        {
                            Log.Write(LogComponent.EthernetController, "EBFCT: DataLate {0} IOCMD {1} Done {2}", _ethernetController.DataLate, _ethernetController.IOCMD, _ethernetController.OperationDone);
                            _nextModifier |= 0x4;
                        }

                        if (_ethernetController.Collision)
                        {
                            Log.Write(LogComponent.EthernetController, "EBFCT: Collision");
                            _nextModifier |= 0x8;
                        }
                        break;

                    case EthernetF2.ECBFCT:
                        // Countdown Branch Function. ORs a one into NEXT[7] if the FIFO is not
                        // empty.
                        if (!_ethernetController.FIFOEmpty)
                        {
                            Log.Write(LogComponent.EthernetController, "ECBFCT: FIFO not empty");
                            _nextModifier |= 0x4;
                        }
                        break;

                    case EthernetF2.EISFCT:
                        // Input Start Function. Sets the IBusy flip flop in the interface, causing it to
                        // hunt for the beginning of a packet: silence on the Ether followed by a
                        // transition. When the interface has collected two words, it will begin
                        // generating data wakeups to the microcode.
                        _ethernetController.StartInput();
                        break;

                    default:
                        throw new NotImplementedException(String.Format("Unimplemented Ethernet F2 {0}", ef2));

                }
            }

            private EthernetController _ethernetController;
        }        
    }
}
