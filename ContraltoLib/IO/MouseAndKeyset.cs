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
using Contralto.Memory;
using Contralto.Scripting;
using System.Diagnostics.CodeAnalysis;

namespace Contralto.IO
{
    [Flags]
    public enum AltoMouseButton
    {
        None = 0x0,
        Middle = 0x1,
        Right = 0x2,
        Left = 0x4,
    }

    [Flags]
    public enum AltoKeysetKey
    {
        None =    0x00,
        Keyset0 = 0x80,     // left-most (bit 8)
        Keyset1 = 0x40,
        Keyset2 = 0x20,
        Keyset3 = 0x10,
        Keyset4 = 0x08,     // right-most (bit 12)
    }

    /// <summary>
    /// Implements the hardware for the standard Alto mouse
    /// and the Keyset, because both share the same memory-mapped
    /// address.  When the Diablo printer is finally emulated,
    /// I'll have to revisit this scheme because it ALSO shares
    /// the same address and that's just silly.
    /// </summary>
    public class MouseAndKeyset : IMemoryMappedDevice
    {
        public MouseAndKeyset(AltoSystem system)
        {
            _system = system;
            _lock = new ReaderWriterLockSlim();
            Reset();
        }

        [MemberNotNull(nameof(_moves))]
        public void Reset()
        {
            _keyset = 0;
            _buttons = AltoMouseButton.None;
            _moves = new Queue<MouseMovement>();
            _currentMove = null;
            _pollCounter = 0;
        }

        public ushort Read(int address, TaskType task, bool extendedMemoryReference)
        {
            return (ushort)~((int)_buttons | (int)_keyset);
        }

        public void Load(int address, ushort data, TaskType task, bool extendedMemoryReference)
        {
            // nothing
        }

        public void MouseMove(int dx, int dy)
        {
            // Calculate number of steps in x and y to be decremented every call to PollMouseBits
            MouseMovement nextMove = new MouseMovement(dx, dy);

            _lock.EnterWriteLock();

                _moves.Enqueue(nextMove);

                if (ScriptManager.IsRecording)
                {
                    ScriptManager.Recorder?.MouseMoveRelative(dx, dy);
                }

            _lock.ExitWriteLock();
        }

        public void MouseMoveAbsolute(int x, int y)
        {
            // We can't /really/ tell the Alto's mouse to move to a specific absolute coordinate.
            // We could shove the coordinates into the well-defined MOUSEX/MOUSEY locations in memory
            // but it feels wrong, somehow.  We can do it slightly less awfully by instead *reading*
            // the coordinates from memory and submitting a relative move based on that.
            // TODO: move memory locations to constants.
            MouseMovement nextMove = new MouseMovement(x, y, false);
            _lock.EnterWriteLock();
                _moves.Enqueue(nextMove);

                if (ScriptManager.IsRecording)
                {
                    ScriptManager.Recorder?.MouseMoveAbsolute(x, y);
                }

            _lock.ExitWriteLock();
        }

        public void MouseDown(AltoMouseButton button)
        {
            _buttons |= button;

            if (ScriptManager.IsRecording)
            {
                ScriptManager.Recorder?.MouseDown(button);
            }
        }

        public void MouseUp(AltoMouseButton button)
        {
            _buttons ^= button;

            if (ScriptManager.IsRecording)
            {
                ScriptManager.Recorder?.MouseUp(button);
            }
        }

        public void KeysetDown(AltoKeysetKey key)
        {
            _keyset |= key;
        }

        public void KeysetUp(AltoKeysetKey key)
        {
            _keyset ^= key;
        }

        /// <summary>
        /// Gets the bits read by the "<-MOUSE" special function, and moves
        /// the pointer one step closer to its final destination (if it has moved at all).
        /// </summary>
        /// <returns></returns>
        public ushort PollMouseBits()
        {
            //
            // The bits returned correspond to the delta incurred by mouse movement in the X and Y direction
            // and map to:
            // 0 : no change in X or Y
            // 1 : dy = -1
            // 2 : dy = 1
            // 3 : dx = -1
            // 4 : dy = -1, dx = -1
            // 5 : dy = 1, dx = -1
            // 6 : dx = 1
            // 7 : dy = -1, dx = 1
            // 8 : dy = 1, dx =1
            ushort bits = 0;

            _lock.EnterReadLock();

            if (_currentMove == null && _moves.Count > 0)
            {
                _currentMove = _moves.Dequeue();

                if (_currentMove.IsAbsolute)
                {
                    // Convert this to a relative movement, we do this by looking at the current values of
                    // MOUSEX and MOUSEY in the well-defined MOUSELOC locations in memory.
                    int mx = Math.Min(605, (int)_system.Memory.Read(0x114, CPU.TaskType.DisplayHorizontal, false));
                    int my = Math.Min(807, (int)_system.Memory.Read(0x115, CPU.TaskType.DisplayHorizontal, false));
                    _currentMove.MakeRelativeTo(mx, my);
                }
            }

            //
            // <-MOUSE is invoked by the Memory Refresh Task once per scanline (including during vblank) which
            // works out to about 13,000 times a second.  To more realistically simulate the movement of a mouse
            // across a desk, we return actual mouse movement data only periodically.
            //
            if (_currentMove != null && (_pollCounter % _currentMove.PollRate) == 0)
            {
                //
                // Choose a direction.  We do not provide movements in both X and Y at the same time;
                // this is solely to avoid a microcode bug that causes erroneous movements in such cases
                // (which then plays havoc with scripting and absolute coordinates.)
                // (It is also the case that on the real hardware, such movements are extremely rare due to
                // the nature of the hardware involved).
                //
                int dx = _currentMove.DX;
                int dy = _currentMove.DY;

                if (dx != 0 && dy != 0)
                {
                    // Choose just one of the two directions to move in.
                    if (_currentDirection)
                    {
                        dx = 0;
                    }
                    else
                    {
                        dy = 0;
                    }

                    _currentDirection = !_currentDirection;
                }

                if (dy == -1 && dx == 0)
                {
                    bits = 1;
                }
                else if (dy == 1 && dx == 0)
                {
                    bits = 2;
                }
                else if (dy == 0 && dx == -1)
                {
                    bits = 3;
                }
                else if (dy == -1 && dx == -1)
                {
                    bits = 4;
                }
                else if (dy == 1 && dx == -1)
                {
                    bits = 5;
                }
                else if (dy == 0 && dx == 1)
                {
                    bits = 6;
                }
                else if (dy == -1 && dx == 1)
                {
                    bits = 7;
                }
                else if (dy == 1 && dx == 1)
                {
                    bits = 8;
                }

                //
                // Move the mouse closer to its destination in either X or Y
                // (but not both)
                if (_currentMove.XSteps > 0 && dx != 0)
                {
                    _currentMove.XSteps--;

                    if (_currentMove.XSteps == 0)
                    {
                        _currentMove.DX = 0;
                    }
                }

                if (_currentMove.YSteps > 0 && dy != 0)
                {
                    _currentMove.YSteps--;

                    if (_currentMove.YSteps == 0)
                    {
                        _currentMove.DY = 0;
                    }
                }

                if (_currentMove.XSteps == 0 && _currentMove.YSteps == 0)
                {
                    _currentMove = null;
                }
            }
            
            _lock.ExitReadLock();
            _pollCounter++;

            return bits;
        }

        public MemoryRange[] Addresses
        {
            get { return _addresses; }
        }

        private readonly MemoryRange[] _addresses =
        {
            new MemoryRange(0xfe18, 0xfe1b), // UTILIN: 177030-177033
        };

        AltoSystem _system;

        // Mouse buttons:
        AltoMouseButton _buttons;

        // Keyset switches:
        AltoKeysetKey _keyset;

        private ReaderWriterLockSlim _lock;

        // Used to control the rate of mouse movement data
        //
        public int _pollCounter;

        /// <summary>
        /// Where the mouse is moving to every time PollMouseBits is called.
        /// </summary> 
        private Queue<MouseMovement> _moves;
        private MouseMovement? _currentMove;
        private bool _currentDirection;

        private class MouseMovement
        {
            public MouseMovement(int x, int y, bool relative = true)
            {
                if (relative)
                {
                    BuildRelative(x, y);
                }
                else
                {
                    AbsX = x;
                    AbsY = y;
                    PollRate = 1;
                }

                IsAbsolute = !relative;
            }


            public void MakeRelativeTo(int absX, int absY)
            {
                if (IsAbsolute)
                {
                    int dx = AbsX - absX;
                    int dy = AbsY - absY;
                    BuildRelative(dx, dy);
                }
            }

            private void BuildRelative(int dx, int dy)
            {
                XSteps = Math.Abs(dx);
                YSteps = Math.Abs(dy);
                DX = Math.Sign(dx);
                DY = Math.Sign(dy);

                //
                // Calculate the rate at which mouse data should be returned in PollMouseBits,
                // this is a function of the distance moved in this movement.  We assume that the
                // movement occurred in 1/60th of a second; PollMouseBits is invoked (via <-MOUSE)
                // by the MRT approximately every 1/13000th of a second.
                // This is all approximate and not expected to be completely accurate.
                //
                double distance = Math.Sqrt(Math.Pow(XSteps, 2) + Math.Pow(YSteps, 2));

                PollRate = (int)((13000.0 / 60.0) / (distance + 1));

                if (PollRate == 0)
                {
                    PollRate = 1;
                }

                IsAbsolute = false;

            }

            public int XSteps;
            public int YSteps;
            public int DX;
            public int DY;
            public int AbsX;
            public int AbsY;
            public int PollRate;
            public bool IsAbsolute;
        }

    }
}

