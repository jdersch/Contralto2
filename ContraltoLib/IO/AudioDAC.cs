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

namespace Contralto.IO
{
    /// <summary>
    /// Implements the hardware for the Audio DAC used by Ted Kaehler's
    /// ST-74 Music System (either the FM-synthesis "TWANG" system or the
    /// Sampling system.)    
    /// </summary>
    public class AudioDAC : IMemoryMappedDevice
    {
        public AudioDAC()
        {

        }

        public void AttachSink(IAudioSink sink)
        {
            _audioSink = sink;
        }

        public void Shutdown()
        {
            if (_audioSink != null)
            {
                _audioSink.Shutdown();
            }
        }

        /// <summary>
        /// Comments in the FM synthesis microcode indicate:
        /// "240 SAMPLES = 18 msec"
        /// Which works out to about 13.3333...Khz.
        /// 
        /// Unsure if this value also applies to the Sampling microcode, but
        /// it sounds about right in action.
        /// 
        /// TODO: This is set to 13.3Khz below to allow audio production to keep up with
        /// the consumption on the host output side; this may be an indication that our
        /// execution frequency is slightly off.  This should be investigated.
        /// </summary>
        public static readonly int AudioDACSamplingRate = 13000;

        /// <summary>
        /// Reads a word from the specified address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="extendedMemory"></param>
        /// <returns></returns>
        public ushort Read(int address, TaskType task, bool extendedMemory)
        {            
            // The DAC is, as far as I can tell, write-only.
            return 0;
        }

        /// <summary>
        /// Writes a word to the specified address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public void Load(int address, ushort data, TaskType task, bool extendedMemory)
        {
            if (_audioSink != null)
            {
                _audioSink.WriteSample(data);
            }
        }

        /// <summary>
        /// Specifies the range (or ranges) of addresses decoded by this device.
        /// </summary>
        public MemoryRange[] Addresses
        {
            get { return _addresses; }
        }

        /// <summary>
        /// From: http://bitsavers.org/pdf/xerox/alto/memos_1975/Reserved_Alto_Memory_Locations_Jan75.pdf
        ///         
        /// #177776: Digital-Analog Converter (DAC Hardware - Kaehler)
        /// </summary>
        private readonly MemoryRange[] _addresses =
        {
            new MemoryRange(0xfffe, 0xfffe),
        };

        private IAudioSink? _audioSink;
    }

}
