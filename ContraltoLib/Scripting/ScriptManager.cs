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
    public static class ScriptManager
    {
        static ScriptManager()
        {
            _scheduler = new Scheduler();
        }

        public static Scheduler ScriptScheduler
        {
            get { return _scheduler; }
        }

        /// <summary>
        /// Fired when playback of a script has completed or is stopped.
        /// </summary>
        public static event EventHandler? PlaybackCompleted;

        public static void StartRecording(AltoSystem system, string scriptPath)
        {
            // Stop any pending actions
            StopRecording();
            StopPlayback();

            _scriptRecorder = new ScriptRecorder(system, scriptPath);

            Log.Write(LogComponent.Scripting, "Starting recording to {0}", scriptPath);

            //
            // Record the absolute position of the mouse (as held in MOUSELOC in system memory).
            // All other mouse movements in the script will be recorded relative to this point.
            //
            int x = system.Memory.Read(0x114, CPU.TaskType.Ethernet, false);
            int y = system.Memory.Read(0x115, CPU.TaskType.Ethernet, false);
            _scriptRecorder.MouseMoveAbsolute(x, y);
        }

        public static void StopRecording()
        {
            if (IsRecording)
            {
                _scriptRecorder?.End();
                _scriptRecorder = null;
            }

            Log.Write(LogComponent.Scripting, "Stopped recording.");
        }

        public static void StartPlayback(AltoSystem system, string scriptPath)
        {
            // Stop any pending actions
            StopRecording();
            StopPlayback();

            _scheduler.Reset();

            _scriptPlayback = new ScriptPlayback(scriptPath, system);
            _scriptPlayback.PlaybackCompleted += OnPlaybackCompleted;
            _scriptPlayback.Start();

            Log.Write(LogComponent.Scripting, "Starting playback of {0}", scriptPath);
        }
        
        public static void StopPlayback()
        {
            if (IsPlaying)
            {
                _scriptPlayback?.Stop();
                _scriptPlayback = null;

                PlaybackCompleted?.Invoke(null, null!);
            }

            Log.Write(LogComponent.Scripting, "Stopped playback.");
        }

        public static void CompleteWait()
        {
            if (IsPlaying)
            {
                _scriptPlayback?.Start();

                Log.Write(LogComponent.Scripting, "Playback resumed after Wait.");
            }
        }

        public static ScriptRecorder? Recorder
        {
            get { return _scriptRecorder; }
        }

        public static ScriptPlayback? Playback
        {
            get { return _scriptPlayback; }
        }

        public static bool IsRecording
        {
            get { return _scriptRecorder != null; }
        }

        public static bool IsPlaying
        {
            get { return _scriptPlayback != null; }
        }

        private static void OnPlaybackCompleted(object? sender, EventArgs e)
        {
            _scriptPlayback = null;
            PlaybackCompleted?.Invoke(null, null!);
        }

        private static ScriptRecorder? _scriptRecorder;
        private static ScriptPlayback? _scriptPlayback;

        private static Scheduler _scheduler;
    }
}
