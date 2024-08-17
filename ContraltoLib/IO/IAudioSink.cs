using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contralto.IO
{
    /// <summary>
    /// IAudioSink provides a method for outputting audio samples to a variety of targets.
    /// </summary>
    public interface IAudioSink
    {
        /// <summary>
        /// Writes a single audio sample to the sink.
        /// </summary>
        /// <param name="sample"></param>
        void WriteSample(ushort sample);

        void Shutdown();
    }
}
