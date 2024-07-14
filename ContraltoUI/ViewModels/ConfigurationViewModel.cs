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

using Avalonia.Data;
using Avalonia.Platform.Storage;
using Contralto;
using ReactiveUI;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace ContraltoUI.ViewModels
{
    public partial class ConfigurationViewModel : ViewModelBase
    {
        public ConfigurationViewModel(AltoSystem system)
        {
            _system = system;

            // Make a copy of the configuration for editing:
            _newConfiguration = new Configuration(_system.Configuration);

            BrowseForDACOutput = ReactiveCommand.Create(OnBrowseForDACOutput);
            BrowseForPDFOutput = ReactiveCommand.Create(OnBrowseForPDFOutput);
        }

        public ICommand BrowseForDACOutput { get; }
        public ICommand BrowseForPDFOutput { get; }

        private async void OnBrowseForDACOutput()
        {
            var files = await FindWindowByViewModel(this).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = $"Select diirectory for DAC output",
                AllowMultiple = false
            });

            if (files.Count == 0)
            {
                return;
            }

            AudioDACCapturePath = files.First().Path.AbsolutePath;
        }

        private async void OnBrowseForPDFOutput()
        {
            var files = await FindWindowByViewModel(this).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = $"Select diirectory for PDF output",
                AllowMultiple = false
            });

            if (files.Count == 0)
            {
                return;
            }

            PDFOutputPath = files.First().Path.AbsolutePath;
        }


        public bool CommitChanges()
        {
            _system.Configuration = _newConfiguration;
            return true;
        }

        public override void OnApplicationExit()
        {

        }

        public string ContraltoVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public IEnumerable<string> NetworkDevices
        {
            get
            {
                return CaptureDeviceList.Instance.Select(d => d.Description);
            }
        }

        public bool RestartRequired
        {
            get
            {
                // TODO: how many of these can we eliminate
                return (HostPacketInterfaceName != _system.Configuration.HostPacketInterfaceName ||
                        HostPacketInterfaceType != _system.Configuration.HostPacketInterfaceType) ||
                        _newConfiguration.HostAddress != _system.Configuration.HostAddress ||
                        SystemType != _system.Configuration.SystemType;
            }
        }

        public SystemType SystemType
        {
            get { return _newConfiguration.SystemType; }
            set
            {
                _newConfiguration.SystemType = value;
                OnPropertyChanged(nameof(SystemType));
                OnPropertyChanged(nameof(RestartRequired));
            }
        }

        public string HostAddress
        {
            get { return Conversion.ToOctal(_newConfiguration.BootAddress); }
            set
            {
                try
                {
                    byte newAddress = Convert.ToByte(value, 8);
                    if (newAddress == 0 || newAddress == 255)
                    {
                        throw new ArgumentException(nameof(HostAddress));
                    }
                    _newConfiguration.HostAddress = newAddress;
                }
                catch
                {
                    throw new DataValidationException("Value must be specified in octal and be between 1 and 376.");
                }
                OnPropertyChanged(nameof(HostAddress));
            }
        }

        public PacketInterfaceType HostPacketInterfaceType
        {
            get { return _newConfiguration.HostPacketInterfaceType; }
            set
            {
                _newConfiguration.HostPacketInterfaceType = value;
                OnPropertyChanged(nameof(HostPacketInterfaceType));
                OnPropertyChanged(nameof(RestartRequired));
            }
        }

        public string HostPacketInterfaceName
        {
            get { return _newConfiguration.HostPacketInterfaceName; }
            set
            {
                _newConfiguration.HostPacketInterfaceName = value;
                OnPropertyChanged(nameof(HostPacketInterfaceName));
                OnPropertyChanged(nameof(RestartRequired));
            }
        }

        public IEnumerable<string> AvailableHostPacketInterfaces
        {
            get
            {
                // TODO: enumerate interfaces using whatever cross-platform network lib we decide upon
                return null;
            }
        }

        public bool ThrottleSpeed
        {
            get { return _newConfiguration.ThrottleSpeed; }
            set
            {
                _newConfiguration.ThrottleSpeed = value;
                OnPropertyChanged(nameof(ThrottleSpeed));
            }
        }

        public bool SlowPhosphorSimulation
        {
            get { return _newConfiguration.SlowPhosphorSimulation; }
            set
            {
                _newConfiguration.SlowPhosphorSimulation = value;
                OnPropertyChanged(nameof(SlowPhosphorSimulation));
            }
        }

        public int DisplayScale
        {
            get { return _newConfiguration.DisplayScale; }
            set
            {
                _newConfiguration.DisplayScale = value;
                OnPropertyChanged(nameof(DisplayScale));
            }
        }

        public bool EnableAudioDAC
        {
            get { return _newConfiguration.EnableAudioDAC; }
            set
            {
                _newConfiguration.EnableAudioDAC = value;
                OnPropertyChanged(nameof(EnableAudioDAC));
            }
        }

        public bool EnableAudioDACCapture
        {
            get { return _newConfiguration.EnableAudioDACCapture; }
            set
            {
                _newConfiguration.EnableAudioDACCapture = value;
                OnPropertyChanged(nameof(EnableAudioDACCapture));
            }
        }

        public string AudioDACCapturePath
        {
            get { return _newConfiguration.AudioDACCapturePath; }
            set
            {
                _newConfiguration.AudioDACCapturePath = value;
                OnPropertyChanged(nameof(AudioDACCapturePath));
            }
        }

        public bool EnablePrinting
        {
            get { return _newConfiguration.EnablePrinting; }
            set
            {
                _newConfiguration.EnablePrinting = value;
                OnPropertyChanged(nameof(EnablePrinting));
            }
        }

        public bool ReversePageOrder
        {
            get { return _newConfiguration.ReversePageOrder; }
            set
            {
                _newConfiguration.ReversePageOrder = value;
                OnPropertyChanged(nameof(ReversePageOrder));
            }
        }

        public string PDFOutputPath
        {
            get { return _newConfiguration.PrintOutputPath; }
            set
            {
                _newConfiguration.PrintOutputPath = value;
                OnPropertyChanged(nameof(PDFOutputPath));
            }
        }

        public int PageRasterOffsetX
        {
            get { return _newConfiguration.PageRasterOffsetX; }
            set
            {
                _newConfiguration.PageRasterOffsetX = value;
                OnPropertyChanged(nameof(PageRasterOffsetX));
            }
        }

        public int PageRasterOffsetY
        {
            get { return _newConfiguration.PageRasterOffsetY; }
            set
            {
                _newConfiguration.PageRasterOffsetY = value;
                OnPropertyChanged(nameof(PageRasterOffsetY));
            }
        }

        public AlternateBootType AlternateBootType
        {
            get { return _newConfiguration.AlternateBootType; }
            set
            {
                _newConfiguration.AlternateBootType = value;
                OnPropertyChanged(nameof(AlternateBootType));
            }
        }

        public string DiskBootAddress
        {
            get { return Conversion.ToOctal(_newConfiguration.BootAddress); }
            set
            {
                try
                {
                    _newConfiguration.BootAddress = Convert.ToUInt16(value, 8);
                }
                catch
                {
                    throw new DataValidationException("Value must be specified in octal and be between 0 and 177777.");
                }
                OnPropertyChanged(nameof(DiskBootAddress));
            }
        }

        public BootFileEntry EtherBootFile
        {
            get {
                return _bootEntries.Where(id => id.FileNumber == _newConfiguration.BootFile).FirstOrDefault(); 
            }
            set
            {
                _newConfiguration.BootFile = value.FileNumber;
                OnPropertyChanged(nameof(EtherBootFile));
            }
        }

        public BootFileEntry[] BootEntries
        {
            get { return _bootEntries; }
        }
            
        private BootFileEntry[] _bootEntries = new BootFileEntry[]
        {
            new BootFileEntry(0, "DMT"),
            new BootFileEntry(1, "NewOS"),
            new BootFileEntry(2, "FTP"),
            new BootFileEntry(3, "Scavenger"),
            new BootFileEntry(4, "CopyDisk"),
            new BootFileEntry(5, "CRTTest"),
            new BootFileEntry(6, "MADTest"),
            new BootFileEntry(7, "Chat"),
            new BootFileEntry(8, "NetExec"),
            new BootFileEntry(9, "PupTest"),
            new BootFileEntry(10, "EtherWatch"),
            new BootFileEntry(11, "KeyTest"),
            new BootFileEntry(13, "DiEx"),
            new BootFileEntry(15, "EDP"),
            new BootFileEntry(16, "BFSTest"),
            new BootFileEntry(17, "GateControl"),
            new BootFileEntry(18, "EtherLoad"),
        };

        private AltoSystem _system;
        private Configuration _newConfiguration;
    }

    public struct BootFileEntry
    {
        public BootFileEntry(ushort number, string desc)
        {
            FileNumber = number;
            Description = desc;
        }

        public override string ToString()
        {
            return String.Format("{0} - {1}", Conversion.ToOctal(FileNumber), Description);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public ushort FileNumber;
        public string Description;
    }
}
