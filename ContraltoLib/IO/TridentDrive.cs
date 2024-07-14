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

namespace Contralto.IO
{
    /// <summary>
    /// Encapsulates logic that belongs to a Trident drive, including loading/saving packs,
    /// seeking, and parceling out sector data.
    /// </summary>
    public class TridentDrive
    {
        public TridentDrive(AltoSystem system)
        {
            _system = system;

            _seekEvent = new Event(0, null, SeekCallback);
            Reset();
        }

        public void Reset()
        {
            _sector = 0;
            _cylinder = 0;
            _head = 0;

            _notReady = false;

            UpdateTrackCache();
        }

        public void NewPack(string path, DiskGeometry geometry)
        {
            if (_pack != null)
            {
                UpdateTrackCache();
                _pack.Dispose();
            }

            _pack = FileBackedDiskPack.CreateEmpty(geometry, path);
            Reset();
        }

        public void LoadPack(IDiskPack pack)
        {
            if (_pack != null)
            {
                UpdateTrackCache();
                _pack.Dispose();
            }

            _pack = pack;
            Reset();
        }

        public void UnloadPack()
        {
            if (_pack != null)
            {
                UpdateTrackCache();
                _pack.Dispose();
            }

            _pack = null;
            Reset();
        }

        public bool IsLoaded
        {
            get { return _pack != null; }
        }

        public bool NotReady
        {
            get { return _notReady; }
        }

        public IDiskPack Pack
        {
            get { return _pack; }
        }

        public int Sector
        {
            get { return _sector; }
            set { _sector = value; }
        }       

        public int Head
        {
            get { return _head; }
            set
            {
                if (_head != value)
                {
                    _head = value;
                    UpdateTrackCache();
                }
            }
        }
        
        public int Cylinder
        {
            get { return _cylinder; }
            set
            {
                if (_cylinder != value)
                {
                    _cylinder = value;
                    UpdateTrackCache();
                }
            }
        }

        public bool ReadOnly
        {
            get { return false; }
        }

        public bool Seek(int destCylinder)
        {
            if (destCylinder > _pack.Geometry.Cylinders - 1)
            {
                Log.Write(LogType.Error, LogComponent.TridentController, "Specified cylinder {0} is out of range.  Seek aborted, device check raised.", destCylinder);                
                return false;
            }

            int currentCylinder = _cylinder;
            if (destCylinder != currentCylinder)
            {
                // Do a seek.
                _notReady = true;

                _destCylinder = destCylinder;

                //
                // I can't find a specific formula for seek timing; the Century manual says:
                // "Positioning time for seeking to the next cylinder is normally 6ms, and
                //  for full seeks (814 cylinder differerence) it is 55ms."
                //
                // I'm just going to fudge this for now and assume a linear ramp; this is not
                // accurate but it's not all that important.
                //
                _seekDuration = (ulong)((6.0 + 0.602 * Math.Abs(currentCylinder - destCylinder)) * Conversion.MsecToNsec);

                Log.Write(LogComponent.TridentController, "Commencing seek from {0} to {1}.  Seek time is {2}ns", destCylinder, currentCylinder, _seekDuration);

                _seekEvent.TimestampNsec = _seekDuration;
                _system.Scheduler.Schedule(_seekEvent);
            }
            else
            {
                Log.Write(LogComponent.TridentController, "Seek is a no-op.");
            }

            return true;
        }        

        public ushort ReadHeader(int wordOffset)
        {
            if (wordOffset >= SectorGeometry.Trident.HeaderSize)
            {
                //
                // We just ignore this; the microcode may read extra words
                // and the controller was expected to ignore them.
                //
                Log.Write(LogType.Warning, LogComponent.TridentDisk, "Extra header word read, offset {0}", wordOffset);
                return 0;
            }
            else
            {
                return CurrentSector.ReadHeader(wordOffset);
            }
        }

        public ushort ReadLabel(int wordOffset)
        {
            if (wordOffset >= SectorGeometry.Trident.LabelSize)
            {
                //
                // We just ignore this; the microcode may read extra words
                // and the controller was expected to ignore them.
                //
                Log.Write(LogType.Warning, LogComponent.TridentDisk, "Extra label word read, offset {0}", wordOffset);
                return 0;
            }
            else
            {
                return CurrentSector.ReadLabel(wordOffset);
            }
        }

        public ushort ReadData(int wordOffset)
        {
            if (wordOffset >= SectorGeometry.Trident.DataSize)
            {
                //
                // We just ignore this; the microcode may read extra words
                // and the controller was expected to ignore them.
                //
                Log.Write(LogType.Warning, LogComponent.TridentDisk, "Extra data word read, offset {0 }", wordOffset);
                return 0;
            }
            else
            {
                return CurrentSector.ReadData(wordOffset);
            }
        }

        public void WriteHeader(int wordOffset, ushort word)
        {            
            if (wordOffset >= SectorGeometry.Trident.HeaderSize)
            {
                //
                // We just ignore this; the microcode may send extra words
                // and the controller was expected to ignore them.
                //
                Log.Write(LogType.Warning, LogComponent.TridentDisk, "Extra header word ({0}) written, offset {1}", Conversion.ToOctal(word), wordOffset);
            }
            else
            {
                CurrentSector.WriteHeader(wordOffset, word);
            }
        }

        public void WriteLabel(int wordOffset, ushort word)
        {
            if (wordOffset >= SectorGeometry.Trident.LabelSize)
            {
                //
                // We just ignore this; the microcode may send extra words
                // and the controller was expected to ignore them.
                //
                Log.Write(LogType.Warning, LogComponent.TridentDisk, "Extra label word ({0}) written, offset {1}", Conversion.ToOctal(word), wordOffset);
            }
            else
            {
                CurrentSector.WriteLabel(wordOffset, word);
            }
        }

        public void WriteData(int wordOffset, ushort word)
        {
            if (wordOffset >= SectorGeometry.Trident.DataSize)
            {
                //
                // We just ignore this; the microcode may send extra words
                // and the controller was expected to ignore them.
                //
                Log.Write(LogType.Warning, LogComponent.TridentDisk, "Extra data word ({0}) written, offset {1}", Conversion.ToOctal(word), wordOffset);
            }
            else
            {
                CurrentSector.WriteData(wordOffset, word);
            }
        }

        private void SeekCallback(ulong skewNsec, object context)
        {
            Log.Write(LogComponent.TridentDisk, "Seek to {0} complete.", _destCylinder);

            Cylinder = _destCylinder;
            _notReady = false;
        }

        //
        // Sector management.  We load in an entire track's worth of sectors at a time.
        // When the head or cylinder changes, UpdateTrackCache must be called.
        //
        private void UpdateTrackCache()
        {
            if (_pack != null)
            {
                if (_trackCache == null)
                {
                    //
                    // First time through, initialize the cache.
                    //
                    _trackCache = new DiskSector[_pack.Geometry.Sectors];
                }
                else
                {
                    //
                    // Commit the sectors back to disk before loading in the new ones.
                    //
                    for (int i = 0; i < _trackCache.Length; i++)
                    {
                        _pack.CommitSector(_trackCache[i]);
                    }
                }

                //
                // Load the new sectors for this track.
                //
                for (int i = 0; i < _trackCache.Length; i++)
                {
                    _trackCache[i] = _pack.GetSector(_cylinder, _head, i);
                }
            }
        }

        private DiskSector CurrentSector
        {
            get { return _trackCache[_sector]; }
        }

        private AltoSystem _system;

        //
        // Current disk position
        //
        private int _cylinder;
        private int _head;
        private int _sector;

        // The pack loaded into the drive
        IDiskPack _pack;

        // Drive status
        private bool _notReady;

        // Seek status and control
        private static ulong _seekDuration;
        private Event _seekEvent;
        private int _destCylinder;


        //
        // The track cache
        //
        private DiskSector[] _trackCache;
    }
}
