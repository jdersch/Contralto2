using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Contralto.IO;
using Contralto.Logging;
using SDL2;

namespace ContraltoUI
{
    public class SDLAudioSink : IAudioSink
    {
        public SDLAudioSink()
        {
            InitializeSDL();
            _audioBuffer = new ConcurrentQueue<ushort>();
            _sampleBuffer = new byte[0x10000];
        }

        public void WriteSample(ushort sample)
        {
            _audioBuffer.Enqueue(sample);
        }

        public void Shutdown()
        {
            SDL.SDL_Quit();
        }

        private void InitializeSDL()
        {
            SDL.SDL_SetHint("SDL_WINDOWS_DISABLE_THREAD_NAMING", "1");

            int retVal;
            if ((retVal = SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING)) < 0)
            {
                Log.Write(LogType.Error, LogComponent.DAC, "Failed to initialize SDL, retval 0x{0:x}", retVal);
                return;
            }

            //
            // Initialize SDL Audio:
            //
            SDL.SDL_AudioSpec desired = new SDL.SDL_AudioSpec();

            _audioCallback = AudioCallback;

            desired.freq = AudioDAC.AudioDACSamplingRate;
            desired.format = SDL.AUDIO_U16LSB;              // the default is little-endian but we'll make it explicit.
            desired.channels = 1;
            desired.callback = _audioCallback;
            desired.samples = 4096;

            uint deviceId = 1; 
            if (SDL.SDL_OpenAudio(ref desired, IntPtr.Zero) < 0)
            {
                Log.Write(LogType.Error, LogComponent.DAC, "Failed to open default audio device, error 0x{0:x}", SDL.SDL_GetError());
                return;
            }

            SDL.SDL_PauseAudioDevice(deviceId, 0);
            

            Log.Write(LogComponent.DAC, "SDL Audio initialized, device id {0}", deviceId);
        }

        private void AudioCallback(IntPtr userData, IntPtr stream, int length)
        {
            ushort lastSample = 0;
            for (int i = 0; i < length / 2; i++)
            {
                if (_audioBuffer.TryDequeue(out ushort sample))
                {
                    _sampleBuffer[i * 2] = (byte)sample;
                    _sampleBuffer[i * 2 + 1] = (byte)(sample >> 8);
                    lastSample = sample;
                }
                else
                {
                    // Out of data, just send the last sample to try to reduce popping.
                    _sampleBuffer[i * 2] = (byte)lastSample;
                    _sampleBuffer[i * 2 + 1] = (byte)(lastSample >> 8);
                }
            }

            Marshal.Copy(_sampleBuffer, 0, stream, length);
        }

        /// <summary>
        /// Reused buffer for marshaling to SDL2.
        /// </summary>
        private byte[] _sampleBuffer;

        /// <summary>
        /// Audio samples waiting to be written out.
        /// </summary>
        private ConcurrentQueue<ushort> _audioBuffer;

        //
        // Local reference for the SDL Audio callback:
        // SDL-CS doesn't hold this reference which causes
        // problems when the GC runs.
        //
        private SDL.SDL_AudioCallback _audioCallback;
    }
}
