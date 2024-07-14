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

using Contralto.Display;

namespace Contralto.CPU
{
    public partial class AltoCPU
    {
        /// <summary>
        /// DisplayHorizontalTask provides implementations of the DHT task functions.
        /// </summary>
        private sealed class DisplayHorizontalTask : Task
        {
            public DisplayHorizontalTask(AltoCPU cpu) : base(cpu)
            {
                _taskType = TaskType.DisplayHorizontal;                
                _wakeup = false;

                _displayController = _cpu._system.DisplayController;
            }

            public override void OnTaskSwitch()
            {
                // We put ourselves back to sleep immediately once we've started running.
                _wakeup = false;
            }

            protected override void ExecuteSpecialFunction2(MicroInstruction instruction)
            {
                DisplayHorizontalF2 dh2 = (DisplayHorizontalF2)instruction.F2;
                switch (dh2)
                {
                    case DisplayHorizontalF2.EVENFIELD:
                        _nextModifier |= (ushort)(_displayController.EVENFIELD ? 1 : 0);
                        break;

                    case DisplayHorizontalF2.SETMODE:
                        // "If bit 0 = 1, the bit clock rate is set to 100ns period (at the start of the next scan line),
                        // and a 1 is merged into NEXT[9]."
                        _displayController.SETMODE(_busData);

                        if ((_busData & 0x8000) != 0)
                        {
                            _nextModifier |= 1;
                        }
                        break;

                    default:
                        throw new InvalidOperationException(String.Format("Unhandled display word F2 {0}.", dh2));                        
                }
            }

            protected override void ExecuteBlock()
            {
                _displayController.DHTBLOCK = true;                
            }

            private DisplayController _displayController;
        }
    }
}
