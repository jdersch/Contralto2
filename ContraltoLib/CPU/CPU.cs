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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Contralto.Logging;

namespace Contralto.CPU
{
    public enum TaskType
    {
        Invalid = -1,
        Emulator = 0,
        Orbit = 1,
        TridentOutput = 3,
        DiskSector = 4,
        Ethernet = 7,
        MemoryRefresh = 8,
        DisplayWord = 9,
        Cursor = 10,
        DisplayHorizontal = 11,
        DisplayVertical = 12,
        Parity = 13,
        DiskWord = 14,
        TridentInput = 15,
    }

    public partial class AltoCPU : IClockable
    {
        public AltoCPU(AltoSystem system)
        {
            _system = system;

            _controlROM = new ControlROM(system.Configuration);
            _uCodeMemory = new UCodeMemory(system, _controlROM);

            _tasks[(int)TaskType.Emulator] = new EmulatorTask(this);
            _tasks[(int)TaskType.DiskSector] = new DiskTask(this, true);
            _tasks[(int)TaskType.DiskWord] = new DiskTask(this, false);
            _tasks[(int)TaskType.DisplayWord] = new DisplayWordTask(this);
            _tasks[(int)TaskType.DisplayHorizontal] = new DisplayHorizontalTask(this);
            _tasks[(int)TaskType.DisplayVertical] = new DisplayVerticalTask(this);
            _tasks[(int)TaskType.Cursor] = new CursorTask(this);
            _tasks[(int)TaskType.MemoryRefresh] = new MemoryRefreshTask(this);
            _tasks[(int)TaskType.Ethernet] = new EthernetTask(this);
            _tasks[(int)TaskType.Parity] = new ParityTask(this);
            _tasks[(int)TaskType.Orbit] = new OrbitTask(this);
            _tasks[(int)TaskType.TridentInput] = new TridentTask(this, true);
            _tasks[(int)TaskType.TridentOutput] = new TridentTask(this, false);

            _currentTask = _nextTask = null!;

            Reset();
        }

        public Task[] Tasks
        {
            get { return _tasks; }
        }

        public Task CurrentTask
        {
            get { return _currentTask; }
        }

        public UCodeMemory UCodeMemory
        {
            get { return _uCodeMemory; }
        }

        public ControlROM ControlROM
        {
            get { return _controlROM; }
        }

        public ushort[] R
        {
            get { return _r; }
        }

        public ushort[][] S
        {
            get { return _s; }
        }

        public ushort T
        {
            get { return _t; }
        }

        public ushort L
        {
            get { return _l; }
        }

        public ushort M
        {
            get { return _m; }
        }

        public ushort IR
        {
            get { return _ir; }
        }

        public ushort ALUC0
        {
            get { return _aluC0; }
        }

        [MemberNotNull(nameof(_r), nameof(_s))]
        public void Reset()
        {
            _uCodeMemory.Reset();
            
            _r = new ushort[32];
            _s = new ushort[8][];

            for(int i=0;i<_s.Length;i++)
            {
                _s[i] = new ushort[32];
            }

            _t = 0;
            _l = 0;
            _m = 0;
            _ir = 0;
            _aluC0 = 0;
            _rmr = 0xffff;      // Start all tasks in ROM0

            // Reset tasks.
            for (int i=0;i<_tasks.Length;i++)
            {
                if (_tasks[i] != null)
                {
                    _tasks[i].Reset();
                }
            }

            // Execute the initial task switch.
            TaskSwitch();
            _currentTask = _nextTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clock()
        {
            switch (_currentTask.ExecuteNext())
            {
                case InstructionCompletion.Normal:
                    // If we have a new task, switch to it now.
                    if (_currentTask != _nextTask)
                    {
                        _currentTask = _nextTask;
                        _currentTask.FirstInstructionAfterSwitch = true;
                        _currentTask.OnTaskSwitch();
                    }
                    break;

                case InstructionCompletion.TaskSwitch:
                    // Invoke the task switch, this will take effect after
                    // the NEXT instruction completes, not this one.
                    TaskSwitch();
                    break;                

                case InstructionCompletion.MemoryWait:
                    // We were waiting for memory on this cycle, we do nothing
                    // (no task switch even if one is pending) in this case.
                    break;
            }            
        }

        /// <summary>
        /// "Silent Boot":  (See Section 9.2.2)
        /// Used when a BOOT STARTF is invoked; resets task MPCs and sets
        /// the starting bank (RAM0 or ROM0) appropriately based on the contents
        /// of RMR.
        /// All other register contents are left as-is.
        /// </summary>
        public void SoftReset()
        {
            // Soft-Reset tasks.
            for (int i = 0; i < _tasks.Length; i++)
            {
                if (_tasks[i] != null)
                {
                    _tasks[i].SoftReset();
                }
            }

            Log.Write(LogComponent.CPU, "Silent Boot; microcode banks initialized to {0}", Conversion.ToOctal(_rmr));
            UCodeMemory.LoadBanksFromRMR(_rmr);

            // Booting / soft-reset of the Alto resets the XM bank registers to zero.
            _system.Memory.SoftReset();

            // Reset RMR after reset.
            _rmr = 0xffff;
          
            // Start in Emulator
            _currentTask = _tasks[0];

            //
            // TODO: 
            // This is a hack of sorts, it ensures that the sector task initializes
            // itself as soon as the Emulator task yields after the reset.  (CopyDisk is broken otherwise due to the
            // sector task stomping over the KBLK CopyDisk sets up after the reset.  This is a race of sorts.)
            // Unsure if there is a deeper issue here or if there are other reset semantics
            // in play that are not understood.
            //
            WakeupTask(CPU.TaskType.DiskSector);
            BlockTask(CPU.TaskType.DiskWord);

            BlockTask(CPU.TaskType.TridentInput);
            BlockTask(CPU.TaskType.TridentOutput);
        }

        /// <summary>
        /// Used by hardware devices to cause a specific task to have its
        /// "wakeup" signal triggered
        /// </summary>
        /// <param name="task"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WakeupTask(TaskType task)
        {            
            if (_tasks[(int)task] != null)
            {             
               // Log.Write(LogComponent.TaskSwitch, "Wakeup enabled for Task {0}", task);            
                _tasks[(int)task].WakeupTask();                
            }
        }

        /// <summary>
        /// Used by hardware devices to cause a specific task to have its
        /// "wakeup" signal cleared
        /// </summary>
        /// <param name="task"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BlockTask(TaskType task)
        {
            if (_tasks[(int)task] != null)
            {                
                // Log.Write(LogComponent.TaskSwitch, "Removed wakeup for Task {0}", task);                
                _tasks[(int)task].BlockTask();
            }
        }

        public bool IsBlocked(TaskType task)
        {
            if (_tasks[(int)task] != null)
            {
                return _tasks[(int)task].Wakeup;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Used by the debugger to determine if a task switch is taking
        /// place.
        /// </summary>
        public Task NextTask
        {
            get { return _nextTask; }
        }

        public bool InternalBreak
        {
            get
            {
                return _internalBreak;
            }

            set
            {
                _internalBreak = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private void TaskSwitch()
        {            
            // Select the highest-priority eligible task
            for (int i = _tasks.Length - 1; i >= 0; i--)
            {
                if (_tasks[i] != null && _tasks[i].Wakeup)
                {                    
                    _nextTask = _tasks[i];
                    break;
                }
            }
        }

        // Microcode and constant memory
        UCodeMemory _uCodeMemory;
        ControlROM _controlROM;

        // AltoCPU registers
        private ushort _t;
        private ushort _l;
        private ushort _m;
        private ushort _ir;
        
        // R and S register files and bank select
        private ushort[] _r;
        private ushort[][] _s;

        // Stores the last carry from the ALU on a Load L
        private ushort _aluC0;

        // RMR (Reset Mode Register)
        ushort _rmr;

        // Task data
        private Task _nextTask;         // The task to switch two after the next microinstruction
        private Task _currentTask;      // The currently executing task
        private Task[] _tasks = new Task[16];

        // The system this CPU belongs to
        private AltoSystem _system;

        private bool _internalBreak;
    }
}
