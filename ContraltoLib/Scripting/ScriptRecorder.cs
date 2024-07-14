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

namespace Contralto.Scripting
{
    /// <summary>
    /// Records actions.
    /// </summary>
    public class ScriptRecorder
    {
        public ScriptRecorder(AltoSystem system, string scriptFile)
        {
            _script = new ScriptWriter(scriptFile);
            _system = system;
            _lastTimestamp = 0;

            _firstTime = true;
        }

        public void End()
        {
            _script.End();
        }

        public void KeyDown(AltoKey key)
        {
            _script.AppendAction(
                new KeyAction(
                    GetRelativeTimestamp(),
                    key,
                    true));
        }

        public void KeyUp(AltoKey key)
        {
            _script.AppendAction(
                new KeyAction(
                    GetRelativeTimestamp(),
                    key,
                    false));
        }

        public void MouseDown(AltoMouseButton button)
        {
            _script.AppendAction(
                new MouseButtonAction(
                    GetRelativeTimestamp(),
                    button,
                    true));
        }

        public void MouseUp(AltoMouseButton button)
        {
            _script.AppendAction(
                new MouseButtonAction(
                    GetRelativeTimestamp(),
                    button,
                    false));
        }

        public void MouseMoveRelative(int dx, int dy)
        {
            _script.AppendAction(
                new MouseMoveAction(
                    GetRelativeTimestamp(),
                    dx,
                    dy,
                    false));
        }

        public void MouseMoveAbsolute(int dx, int dy)
        {
            _script.AppendAction(
                new MouseMoveAction(
                    GetRelativeTimestamp(),
                    dx,
                    dy,
                    true));
        }

        public void Command(string command)
        {
            _script.AppendAction(
                new CommandAction(
                    GetRelativeTimestamp(),
                    command));
        }

        private ulong GetRelativeTimestamp()
        {
            if (_firstTime)
            {
                _firstTime = false;
                //
                // First item recorded, occurs at relative timestamp 0.
                //
                _lastTimestamp = ScriptManager.ScriptScheduler.CurrentTimeNsec;
                return 0;
            }
            else
            {
                //
                // relative time is delta between current system timestamp and the last
                // recorded entry.
                ulong relativeTimestamp = ScriptManager.ScriptScheduler.CurrentTimeNsec - _lastTimestamp;
                _lastTimestamp = ScriptManager.ScriptScheduler.CurrentTimeNsec;

                return relativeTimestamp;
            }
        }

        private bool _enabled;

        private AltoSystem _system;
        private ulong _lastTimestamp;
        private bool _firstTime;
        private ScriptWriter _script;
    }
}
