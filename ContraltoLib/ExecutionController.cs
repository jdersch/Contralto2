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

using Contralto.Scripting;

namespace Contralto
{

    public delegate bool StepCallbackDelegate();
    public delegate void ErrorCallbackDelegate(Exception e);
    public delegate void ShutdownCallbackDelegate(bool commitDisks);

    public class ShutdownException : Exception
    {
        public ShutdownException(bool commitDisks) : base()
        {
            _commitDisks = commitDisks;
        }

        public bool CommitDisks
        {
            get { return  _commitDisks; }
        }

        private bool _commitDisks;
    }


    public class ExecutionController
    {
        public ExecutionController(AltoSystem system)
        {
            _system = system;

            _execAbort = false;
            _userAbort = false;
        }

        public void StartExecution(AlternateBootType bootType)
        {
            StartAltoExecutionThread();
            _system.PressBootKeys(bootType);
        }

        public void StartExecution()
        {
            StartAltoExecutionThread();
        }

        public void StopExecution()
        {
            _userAbort = true;

            if (System.Threading.Thread.CurrentThread !=
               _execThread)
            {
                //
                // Call is asynchronous, we will wait for the
                // execution thread to finish.
                //
                if (_execThread != null)
                {
                    _execThread.Join();
                    _execThread = null;
                }
            }
        }

        public void Reset(AlternateBootType bootType)
        {
            if (System.Threading.Thread.CurrentThread ==
                _execThread)
            {
                //
                // Call is from within the execution thread
                // so we can just reset the system without worrying
                // about synchronization.
                //
                _system.Reset();
                _system.PressBootKeys(bootType);
            }
            else
            {
                //
                // Call is asynchronous, we need to stop the
                // execution thread and restart it after resetting
                // the system.
                //
                bool running = IsRunning;

                if (running)
                {
                    StopExecution();
                }
                _system.Reset();
                _system.PressBootKeys(bootType);

                if (running)
                {
                    StartExecution();
                }
            }
        }

        public bool IsRunning
        {
            get { return (_execThread != null && _execThread.IsAlive); }
        }
        
        public StepCallbackDelegate? StepCallback
        {
            get { return _stepCallback; }
            set { _stepCallback = value; }
        }

        public ErrorCallbackDelegate? ErrorCallback
        {
            get { return _errorCallback; }
            set { _errorCallback = value; }
        }

        public ShutdownCallbackDelegate? ShutdownCallback
        {
            get { return _shutdownCallback; }
            set { _shutdownCallback = value; }
        }

        private void StartAltoExecutionThread()
        {
            if (_execThread != null && _execThread.IsAlive)
            {
                return;
            }

            _execAbort = false;
            _userAbort = false;

            _execThread = new Thread(new System.Threading.ThreadStart(ExecuteProc));
            _execThread.Start();
        }

        private void ExecuteProc()
        {
            while (true)
            {
                // Execute a single microinstruction
                try
                {
                    _system.SingleStep();

                    if (ScriptManager.IsPlaying ||
                        ScriptManager.IsRecording)
                    {
                        ScriptManager.ScriptScheduler.Clock();
                    }
                }
                catch(ShutdownException s)
                {
                    //
                    // We will only actually shut down if someone
                    // is listening to this event.
                    //
                    if (_shutdownCallback != null)
                    {
                        _shutdownCallback(s.CommitDisks);
                        _execAbort = true;
                    }
                }
                catch (Exception e)
                {
                    if (_errorCallback != null)
                    {
                        _errorCallback(e);
                    }
                    _execAbort = true;
                }

                if (_stepCallback != null)
                {
                    _execAbort = _stepCallback();
                }

                if (_execAbort || _userAbort)
                {
                    // Halt execution
                    break;
                }
            }            
        }

        // Execution thread and state
        private Thread? _execThread;
        private bool _execAbort;
        private bool _userAbort;

        private StepCallbackDelegate? _stepCallback;
        private ErrorCallbackDelegate? _errorCallback;
        private ShutdownCallbackDelegate? _shutdownCallback;

        private AltoSystem _system;
    }
}
