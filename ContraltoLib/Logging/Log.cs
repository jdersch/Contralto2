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

#define LOGGING_ENABLED

namespace Contralto.Logging
{
    /// <summary>
    /// Specifies a component to specify logging for
    /// </summary>
    [Flags]
    public enum LogComponent
    {
        None = 0,
        EmulatorTask = 0x1,
        DiskSectorTask = 0x2,
        DiskWordTask = 0x4,
        DiskController = 0x8,
        Alu = 0x10,
        Memory = 0x20,
        Keyboard = 0x40,
        Display = 0x80,
        Microcode = 0x100,
        CPU = 0x200,
        EthernetController = 0x400,
        EthernetTask = 0x800,
        TaskSwitch = 0x1000,
        HostNetworkInterface = 0x2000,
        EthernetPacket = 0x4000,
        Configuration = 0x8000,
        DAC = 0x10000,
        Organ = 0x20000,
        Orbit = 0x40000,
        DoverROS = 0x80000,
        TridentTask = 0x100000,
        TridentController = 0x200000,
        TridentDisk = 0x400000,

        Scripting = 0x2000000,
        Debug = 0x40000000,
        All =   0x7fffffff
    }

    /// <summary>
    /// Specifies the type (or severity) of a given log message
    /// </summary>
    [Flags]
    public enum LogType
    {
        None = 0,
        Normal = 0x1,
        Warning = 0x2,
        Error = 0x4,
        Verbose = 0x8,
        All = 0x7fffffff
    }

    /// <summary>
    /// Provides basic functionality for logging messages of all types.
    /// </summary>
    public static class Log
    {
        static Log()
        {
            _components = Configuration.LogComponents;
            _type = Configuration.LogTypes;
            _logText = new List<string>();
        }

        public static event EventHandler Updated;

        public static LogComponent LogComponents
        {
            get { return _components; }
            set { _components = value; }
        }

        public static IEnumerable<string> LogText => _logText;

        public static void Clear()
        {
            _logText.Clear();
        }

#if LOGGING_ENABLED
        /// <summary>
        /// Logs a message without specifying type/severity for terseness;
        /// will not log if Type has been set to None.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Write(LogComponent component, string message, params object[] args)
        {
            Write(LogType.Normal, component, message, args);
        }

        public static void Write(LogType type, LogComponent component, string message, params object[] args)
        {
            if ((_type & type) != 0 &&
                (_components & component) != 0)
            {
                //
                // My log has something to tell you...
                // TODO: color based on type, etc.
                string format = $"{_logText.Count}: {component.ToString()} : {message}";
                string output = String.Format(format, args);
                _logText.Add(output);

                Updated(output, new EventArgs());
            }
        }
#else
        public static void Write(LogComponent component, string message, params object[] args)
        {
            
        }

        public static void Write(LogType type, LogComponent component, string message, params object[] args)
        {

        }

#endif

        private static LogComponent _components;
        private static LogType _type;
        private static long _logIndex;
        private static List<string> _logText;
    }
}
