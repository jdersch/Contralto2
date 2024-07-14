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

using Contralto.Logging;

namespace Contralto.Scripting
{
    public class ScriptPlayback
    {
        public ScriptPlayback(string scriptFile, AltoSystem system)
        {
            _scriptReader = new ScriptReader(scriptFile);
            _system = system;

            _currentAction = null;

            _stopPlayback = false;
        }

        /// <summary>
        /// Fired when playback of the script has completed or is stopped.
        /// </summary>
        public event EventHandler PlaybackCompleted;

        public void Start()
        {
            _stopPlayback = false;

            // Schedule first event.
            ScheduleNextEvent(0);
        }

        public void Stop()
        {
            // We will stop after the next event is fired (if any)
            _stopPlayback = true;
        }

        private void ScheduleNextEvent(ulong skewNsec)
        {
            //
            // Grab the next action if the current one is done.
            //
            if (_currentAction == null || _currentAction.Completed)
            {
                _currentAction = _scriptReader.ReadNext();
            }            
            
            if (_currentAction != null)
            {
                // We have another action to queue up.
                Event scriptEvent = new Event(_currentAction.Timestamp, _currentAction, OnEvent);
                ScriptManager.ScriptScheduler.Schedule(scriptEvent);

                Log.Write(LogComponent.Scripting, "Queueing script action {0}", _currentAction);
            }
            else 
            {
                //
                // Playback is complete.
                //
                Log.Write(LogComponent.Scripting, "Playback completed.");
                PlaybackCompleted(this, null);
            }
        }

        private void OnEvent(ulong skewNsec, object context)
        {
            // Replay the action.
            if (!_stopPlayback)
            {
                ScriptAction action = (ScriptAction)context;
                Log.Write(LogComponent.Scripting, "Invoking action {0}", action);

                action.Replay(_system);

                // Special case for Wait -- this causes the script to stop here until the
                // Alto itself tells things to start up again.
                //
                if (action is WaitAction)
                {
                    Log.Write(LogComponent.Scripting, "Playback paused, awaiting wakeup from Alto.");
                }
                else
                {
                    // Kick off the next action in the script.
                    ScheduleNextEvent(skewNsec);
                }
            }
            else
            {
                Log.Write(LogComponent.Scripting, "Playback stopped.");
                PlaybackCompleted(this, null);
            }
        }

        private AltoSystem _system;
        private ScriptReader _scriptReader;

        private ScriptAction _currentAction;

        private bool _stopPlayback;
    }
}
