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
    /// Defines the "type" of data in the current sector timeslice.
    /// </summary>
    public enum CellType
    {
        Data,
        Gap,
        Sync,
    }

    /// <summary>
    /// Represents the data (or lack thereof) in the current sector timeslice.
    /// This may be actual data (header, label, or data), interrecord gaps, or
    /// sync words.
    /// </summary>
    public struct DataCell
    {
        public DataCell(ushort data, CellType type)
        {
            Data = data;
            Type = type;
        }

        public ushort Data;
        public CellType Type;

        public override string ToString()
        {
            return String.Format("{0} {1}", Data, Type);
        }

        public static DataCell Empty = new DataCell(0, CellType.Data);
    }

    /// <summary>
    /// Encapsulates logic that belongs to the drive, including loading/saving packs,
    /// seeking and reading sector data.
    /// </summary>
    public class DiabloDrive
    {
        public DiabloDrive(AltoSystem system)
        {
            _system = system;           
            Reset();
        }

        public void Reset()
        {
            _sector = 0;
            _cylinder = 0;
            _head = 0;

            _sectorModified = false;
            InitSector();
            LoadSector();
        }

        // Offsets in words for start of data in sector
        public static readonly int HeaderOffset = 44;
        public static readonly int LabelOffset = HeaderOffset + 14;
        public static readonly int DataOffset = LabelOffset + 20;

        // Total "words" (really timeslices) per sector.
        public static readonly int SectorWordCount = 269 + HeaderOffset + 34;

        public void LoadPack(IDiskPack pack)
        {
            if (_pack != null)
            {
                _pack.Dispose();
            }

            _pack = pack;
            Reset();
        }

        public void UnloadPack()
        {
            if (_pack != null)
            {
                _pack.Dispose();
            }

            _pack = null;
            Reset();
        }

        public bool IsLoaded
        {
            get { return _pack != null; }
        }

        public IDiskPack Pack
        {
            get { return _pack; }
        }

        public int Sector
        {
            get { return _sector; }
            set
            {
                // If the last sector was modified,
                // commit it before moving to the next.
                if (_sectorModified)
                {
                    CommitSector();
                    _sectorModified = false;
                }

                _sector = value;
                LoadSector();
            }
        }       

        public int Head
        {
            get { return _head; }
            set
            {
                if (value != _head)
                {
                    // If we switch heads, we need to reload the sector.            

                    // If the last sector was modified,
                    // commit it before moving to the next.
                    if (_sectorModified)
                    {
                        CommitSector();
                        _sectorModified = false;
                    }

                    _head = value;
                    LoadSector();
                }                
            }
        }
        
        public int Cylinder
        {
            get { return _cylinder; }
            set
            {
                if (value != _cylinder)
                {
                    // If we switch cylinders, we need to reload the sector.
                    // If the last sector was modified,
                    // commit it before moving to the next.
                    if (_sectorModified)
                    {
                        CommitSector();
                        _sectorModified = false;
                    }

                    _cylinder = value;
                    LoadSector();                    
                }
            }
        }                

        public DataCell ReadWord(int index)
        {
            if (_pack != null)
            {
                return _sectorData[index];
            }
            else
            {
                return DataCell.Empty;
            }
        }

        public void WriteWord(int index, ushort data)
        {
            if (_pack!= null && index < _sectorData.Length)
            {
                if (_sectorData[index].Type == CellType.Data)
                {
                    _sectorData[index].Data = data;
                }
                else
                {
                    Log.Write(LogType.Warning, LogComponent.DiskController, "Data written to non-data section (Sector {0} Word {1} Rec {2} Data {3})", _sector, index, 666, Conversion.ToOctal(data));
                }
                            
                _sectorModified = true;
            }            
        }
        

        private void LoadSector()
        {
            if (_pack == null)
            {
                return;
            }

            //
            // Pull data off disk and pack it into our faked-up sector.
            // Note that this data is packed in in REVERSE ORDER because that's
            // how it gets written out and it's how the Alto expects it to be read back in.
            //
            DiskSector sector = _pack.GetSector(_cylinder, _head, _sector);

            // Header (2 words data, 1 word cksum)
            for (int i = HeaderOffset + 1, j = 1; i < HeaderOffset + 3; i++, j--)
            {
                // actual data to be loaded from disk / cksum calculated
                _sectorData[i] = new DataCell(sector.ReadHeader(j), CellType.Data);
            }

            ushort checksum = CalculateChecksum(_sectorData, HeaderOffset + 1, 2);
            _sectorData[HeaderOffset + 3].Data = checksum;
            Log.Write(LogType.Verbose, LogComponent.DiskController, "Header checksum for C/H/S {0}/{1}/{2} is {3}", _cylinder, _head, _sector, Conversion.ToOctal(checksum));

            // Label (8 words data, 1 word cksum)
            for (int i = LabelOffset + 1, j = 7; i < LabelOffset + 9; i++, j--)
            {
                // actual data to be loaded from disk / cksum calculated
                _sectorData[i] = new DataCell(sector.ReadLabel(j), CellType.Data);
            }

            checksum = CalculateChecksum(_sectorData, LabelOffset + 1, 8);
            _sectorData[LabelOffset + 9].Data = checksum;
            Log.Write(LogType.Verbose, LogComponent.DiskController, "Label checksum for C/H/S {0}/{1}/{2} is {3}", _cylinder, _head, _sector, Conversion.ToOctal(checksum));

            // sector data (256 words data, 1 word cksum)
            for (int i = DataOffset + 1, j = 255; i < DataOffset + 257; i++, j--)
            {
                // actual data to be loaded from disk / cksum calculated
                _sectorData[i] = new DataCell(sector.ReadData(j), CellType.Data);
            }

            checksum = CalculateChecksum(_sectorData, DataOffset + 1, 256);
            _sectorData[DataOffset + 257].Data = checksum;
            Log.Write(LogType.Verbose, LogComponent.DiskController, "Data checksum for C/H/S {0}/{1}/{2} is {3}", _cylinder, _head, _sector, Conversion.ToOctal(checksum));

        }       

        /// <summary>
        /// Commits modified sector data back to the emulated disk.
        /// Intended to be called at the end of the sector / beginning of the next.
        /// </summary>
        private void CommitSector()
        {
            if (_pack == null)
            {
                return;
            }

            DiskSector sector = _pack.GetSector(_cylinder, _head, _sector);

            // Header (2 words data, 1 word cksum)
            for (int i = HeaderOffset + 1, j = 1; i < HeaderOffset + 3; i++, j--)
            {
                // actual data to be loaded from disk / cksum calculated
                sector.WriteHeader(j, _sectorData[i].Data);
            }

            // Label (8 words data, 1 word cksum)
            for (int i = LabelOffset + 1, j = 7; i < LabelOffset + 9; i++, j--)
            {
                // actual data to be loaded from disk / cksum calculated
                sector.WriteLabel(j, _sectorData[i].Data);
            }

            // sector data (256 words data, 1 word cksum)
            for (int i = DataOffset + 1, j = 255; i < DataOffset + 257; i++, j--)
            {
                // actual data to be loaded from disk / cksum calculated
                sector.WriteData(j, _sectorData[i].Data);
            }

            //
            // Commit it back to the pack.
            //
            _pack.CommitSector(sector);
        }

        private void InitSector()
        {
            // Fill in sector with default data (basically, fill in non-data areas).

            //
            // header delay, 22 words
            for (int i = 0; i < HeaderOffset; i++)
            {
                _sectorData[i] = new DataCell(0, CellType.Gap);
            }

            _sectorData[HeaderOffset] = new DataCell(1, CellType.Sync);
            // inter-reccord delay between header & label (10 words)
            for (int i = HeaderOffset + 4; i < LabelOffset; i++)
            {
                _sectorData[i] = new DataCell(0, CellType.Gap);
            }

            _sectorData[LabelOffset] = new DataCell(1, CellType.Sync);
            // inter-reccord delay between label & data (10 words)
            for (int i = LabelOffset + 10; i < DataOffset; i++)
            {
                _sectorData[i] = new DataCell(0, CellType.Gap);
            }

            _sectorData[DataOffset] = new DataCell(1, CellType.Sync);
            // read-postamble
            for (int i = DataOffset + 258; i < SectorWordCount; i++)
            {
                _sectorData[i] = new DataCell(0, CellType.Gap);
            }
        }

        private ushort CalculateChecksum(DataCell[] sectorData, int offset, int length)
        {
            //
            // From the uCode, the Alto's checksum algorithm is:
            // 1. Load checksum with constant value of 521B (0x151)
            // 2. For each word in the record, cksum <- word XOR cksum
            // 3. Profit
            //
            ushort checksum = 0x151;

            for (int i = offset; i < offset + length; i++)
            {
                // Sanity check that we're checksumming actual data
                if (sectorData[i].Type != CellType.Data)
                {
                    throw new InvalidOperationException("Attempt to checksum non-data area of sector.");
                }

                checksum = (ushort)(checksum ^ sectorData[i].Data);
            }

            return checksum;
        }

        private AltoSystem _system;

        //
        // Current disk position
        //
        private int _cylinder;        
        private int _head;
        private int _sector;       

        private bool _sectorModified;        

        private DataCell[] _sectorData = new DataCell[SectorWordCount];


        // The pack loaded into the drive
        IDiskPack _pack;
    }
}
