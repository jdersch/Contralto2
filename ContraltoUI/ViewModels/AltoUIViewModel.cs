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

using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia;
using Contralto;
using Contralto.Display;
using System.Runtime.InteropServices;
using System;
using Avalonia.Input;
using Contralto.IO;
using System.Collections.Generic;
using ReactiveUI;
using Avalonia.Controls.ApplicationLifetimes;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using System.Linq;
using ContraltoUI.Views;
using System.Threading;
using Contralto.Scripting;

namespace ContraltoUI.ViewModels;

public partial class AltoUIViewModel : ViewModelBase, IAltoDisplay
{
    public AltoUIViewModel(AltoSystem system)
    {
        _hostDesktopDisplayScale = 1.25;
        double compensatedDpi = 96.0 / _hostDesktopDisplayScale;
        _displayBitmap0 = _currentBitmap = new WriteableBitmap(
            new PixelSize(ALTO_DISPLAY_BITMAP_WIDTH, ALTO_DISPLAY_HEIGHT),
            new Vector(compensatedDpi, compensatedDpi),     // DPI
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        _displayBitmap1 = new WriteableBitmap(
            new PixelSize(ALTO_DISPLAY_BITMAP_WIDTH, ALTO_DISPLAY_HEIGHT),
            new Vector(compensatedDpi, compensatedDpi),     // DPI
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        _frameTimer = new HighResolutionTimer(16.6666666f);

        // File menu commands
        LoadDiabloDrive = ReactiveCommand.Create<int>(OnLoadDiabloDrive);
        UnloadDiabloDrive = ReactiveCommand.Create<int>(OnUnloadDiabloDrive);
        NewDiabloDrive = ReactiveCommand.Create<int>(OnNewDiabloDrive);
        LoadTridentDrive = ReactiveCommand.Create<int>(OnLoadTridentDrive);
        UnloadTridentDrive = ReactiveCommand.Create<int>(OnUnloadTridentDrive);
        NewTridentDrive = ReactiveCommand.Create<int>(OnNewTridentDrive);
        SaveScreenshot = ReactiveCommand.Create(OnSaveScreenshot);
        RecordScript = ReactiveCommand.Create(OnRecordScript);
        PlayScript = ReactiveCommand.Create(OnPlayScript);
        Exit = ReactiveCommand.Create(OnExit);

        // System menu commands
        StartSystem = ReactiveCommand.Create(OnStartSystem);
        ResetSystem = ReactiveCommand.Create(OnResetSystem);
        PauseSystem = ReactiveCommand.Create(OnPauseSystem);
        StartSystemWithAltBoot = ReactiveCommand.Create(OnStartSystemWithAltBoot);
        ShowSystemConfigurationDialog = ReactiveCommand.Create(OnShowSystemConfigurationDialog);
        ShowAlternateBootDialog = ReactiveCommand.Create(OnShowAlternateBootDialog);
        ShowDebuggerWindow = ReactiveCommand.Create(OnShowDebuggerWindow);

        // Help menu commands
        ShowAboutDialog = ReactiveCommand.Create(OnShowAboutDialog);

        InitKeymap();

        _system = system;
        _system.AttachDisplay(this);

        _system.Controller.ErrorCallback += OnSystemExecutionError;
        _system.Controller.ShutdownCallback += OnSystemInternalShutdown;

        // Render the initial bitmap to force it to have a size when databound
        Render();

        _uiTimer = new Timer(OnUiTimer, null, 1000, 1000);
    }

    // Databound Properties
    public WriteableBitmap DisplayBitmap
    {
        get { return _displayBitmap; }
        private set
        {
            _displayBitmap = value;
            OnPropertyChanged(nameof(DisplayBitmap));
        }
    }

    // The below properties are used to work around desktop high-DPI scaling so that
    // Alto display pixels map directly to host display pixels without any scaling --
    // as far as I can tell there's no way to ask Avalonia to simply display a bitmap without
    // scaling applied.
    // This way we can apply our own integer-scaling to the display to make it look nice and
    // crisp without bilinear filtering, etc.
    public double HostDesktopDisplayScale
    {
        get { return _hostDesktopDisplayScale; }
        set
        {
            _hostDesktopDisplayScale = value;
            OnPropertyChanged(nameof(HostDesktopDisplayScale));
            OnPropertyChanged(nameof(CompensatedWidth));
            OnPropertyChanged(nameof(CompensatedHeight));
            OnPropertyChanged(nameof(DisplayBitmap));
        }
    }

    public double CompensatedWidth => ((double)ALTO_DISPLAY_BITMAP_WIDTH / _hostDesktopDisplayScale) * _system.Configuration.DisplayScale;
    public double CompensatedHeight => ((double)ALTO_DISPLAY_HEIGHT / _hostDesktopDisplayScale) * _system.Configuration.DisplayScale;

    public bool IsSystemRunning => _system.Controller.IsRunning;

    public double FieldsPerSecond => _fieldsRendered;

    public bool IsScriptRecording => ScriptManager.IsRecording;

    public bool IsScriptPlaying => ScriptManager.IsPlaying;

    public bool CanRecordScript => !ScriptManager.IsRecording && !ScriptManager.IsPlaying;

    public bool CanPlayScript => !ScriptManager.IsRecording && !ScriptManager.IsPlaying;

    public string ExecutionStatus
    {
        get
        {
            if (_system.Controller.IsRunning)
            {
                return "Alto running.";
            }
            else
            {
                return "Alto stopped.";
            }
        }
    }

    public string[] DiabloDriveNames
    {
        get
        {
            string[] names = new string[_system.DiskController.Drives.Length];
            for (int i = 0; i < _system.DiskController.Drives.Length; i++)
            {
                names[i] = _system.DiskController.Drives[i].Pack?.PackName ?? "<no pack loaded>";
            }

            return names;
        }
    }

    public string[] TridentDriveNames
    {
        get
        {
            string[] names = new string[_system.TridentController.Drives.Length];
            for (int i = 0; i < _system.TridentController.Drives.Length; i++)
            {
                names[i] = _system.TridentController.Drives[i].Pack?.PackName ?? "<no pack loaded>";
            }

            return names;
        }
    }

    // Commands
    public ICommand RecordScript { get; }

    private async void OnRecordScript()
    {
        if (!ScriptManager.IsRecording)
        {
            var file = await FindWindowByViewModel(this).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Record New Script",
                ShowOverwritePrompt = true,
                SuggestedFileName = "AltoScript.script",
                FileTypeChoices = new FilePickerFileType[]
                 {
                    new("ContrAlto Scripts")
                    {
                        Patterns = new[] { "*.script" },
                    },
                 }
            });

            if (file == null)
            {
                return;
            }

            ScriptManager.StartRecording(_system, file.Path.AbsolutePath);
        }
        else
        {
            ScriptManager.StopRecording();
        }

        OnPropertyChanged(nameof(CanPlayScript));
        OnPropertyChanged(nameof(CanRecordScript));
        OnPropertyChanged(nameof(IsScriptRecording));
    }

    public ICommand PlayScript { get; }

    private async void OnPlayScript()
    {
        if (!ScriptManager.IsRecording)
        {
            var files = await FindWindowByViewModel(this).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Select Script to Play",
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
            {
                new("ContrAlto Scripts")
                {
                    Patterns = new[] { "*.script" },
                },
            }
            });

            if (files.Count == 0)
            {
                return;
            }

            //
            // Start the script.  We need to pause the emulation while doing so,
            // in order to avoid concurrency issues with the Scheduler (which is
            // not thread-safe).
            //
            _system.Controller.StopExecution();
            ScriptManager.StartPlayback(_system, files.First().Path.AbsolutePath);
            _system.Controller.StartExecution(AlternateBootType.None);
        }
        else
        {
            ScriptManager.StopPlayback();
        }

        OnPropertyChanged(nameof(CanPlayScript));
        OnPropertyChanged(nameof(CanRecordScript));
        OnPropertyChanged(nameof(IsScriptPlaying));
    }

    public ICommand SaveScreenshot { get; }

    private async void OnSaveScreenshot()
    {
        // Pause execution while the user selects the destination for the screenshot
        bool wasRunning = _system.Controller.IsRunning;

        _system.Controller.StopExecution();

        var file = await FindWindowByViewModel(this).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Save screenshot",
            ShowOverwritePrompt = true,
            SuggestedFileName = "AltoScreenshot.png",
            FileTypeChoices = new FilePickerFileType[]
             {
                new("PNG Image")
                {
                    Patterns = new[] { "*.png" },
                },
             }
        });

        if (file == null)
        {
            return;
        }

        try
        {
            DisplayBitmap.Save(file.Path.AbsolutePath, 100);
        }
        catch
        {
            // TODO: AvaloniaUI doesn't provide a MessageBox so... uh, we'll just eat this for now?
        }

        if (wasRunning)
        {
            _system.Controller.StartExecution(AlternateBootType.None);
        }
    }

    public ICommand Exit { get; }

    private void OnExit()
    {
        if (_system.Configuration.KioskMode && !_system.Configuration.AllowKioskExit)
        {
            return;
        }

        OnApplicationExit();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    override public void OnApplicationExit()
    {
        _system.Controller.StopExecution();
        _system.Shutdown(true);

        // Commit configuration back to disk.
        _system.Configuration.WriteConfiguration();
    }

    /// <summary>
    /// Error handling
    /// </summary>
    /// <param name="e"></param>
    private void OnSystemExecutionError(Exception e)
    {
        // If debugging, let the developer have a crack at this
        System.Diagnostics.Debugger.Break();

        // TODO: display some kind of diagnostic?
        OnExit();
    }

    /// <summary>
    /// Handle an internal shutdown of the emulator.
    /// </summary>
    private void OnSystemInternalShutdown(bool commitDisks)
    {
        OnExit();
    }

    public ICommand StartSystem { get; }

    private void OnStartSystem()
    {
        _system.Controller.StartExecution(AlternateBootType.None);
        OnPropertyChanged(nameof(IsSystemRunning));
        OnPropertyChanged(nameof(ExecutionStatus));
    }

    public ICommand ResetSystem { get; }

    private void OnResetSystem()
    {
        _system.Controller.Reset(AlternateBootType.None);
        OnPropertyChanged(nameof(IsSystemRunning));
        OnPropertyChanged(nameof(ExecutionStatus));
    }

    public ICommand PauseSystem { get; }

    private void OnPauseSystem()
    {
        _system.Controller.StopExecution();
        OnPropertyChanged(nameof(IsSystemRunning));
        OnPropertyChanged(nameof(ExecutionStatus));
    }

    public ICommand StartSystemWithAltBoot { get; }

    private void OnStartSystemWithAltBoot()
    {
        if (IsSystemRunning)
        {
            _system.Controller.Reset(_system.Configuration.AlternateBootType);
        }
        else
        {
            _system.Controller.StartExecution(_system.Configuration.AlternateBootType);
        }
        OnPropertyChanged(nameof(IsSystemRunning));
        OnPropertyChanged(nameof(ExecutionStatus));
    }

    public ICommand LoadDiabloDrive { get; }

    private async void OnLoadDiabloDrive(int driveNumber)
    {
        // Start async operation to open the dialog.
        var files = await FindWindowByViewModel(this).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"Load pack for Diablo drive {driveNumber}",
            AllowMultiple = false,
            FileTypeFilter = new FilePickerFileType[] 
            {
                new("Diablo Disks")
                {
                    Patterns = new[] { "*.dsk;*.dsk44" },
                },
                new("Diablo 30") 
                { 
                    Patterns = new[] { "*.dsk" }, 
                }, 
                new("Diablo 44")
                {
                    Patterns = new[] { "*.dsk44" },
                },
                FilePickerFileTypes.All 
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        string imagePath = files.First().Path.AbsolutePath;
        _system.LoadDiabloDrive(driveNumber, imagePath, false);

        // TODO: maybe change the configuration to use a string array like Trident
        if (driveNumber == 0) {
            _system.Configuration.Drive0Image = imagePath;
        }
        else
        {
            _system.Configuration.Drive1Image = imagePath;
        }

        OnPropertyChanged(nameof(DiabloDriveNames));
    }

    public ICommand UnloadDiabloDrive { get; }

    private void OnUnloadDiabloDrive(int driveNumber)
    {
        _system.UnloadDiabloDrive(driveNumber);
        if (driveNumber == 0)
        {
            _system.Configuration.Drive0Image = null;
        }
        else
        {
            _system.Configuration.Drive1Image = null;
        }

        OnPropertyChanged(nameof(DiabloDriveNames));
    }

    public ICommand NewDiabloDrive { get; }

    private async void OnNewDiabloDrive(int driveNumber)
    {
        var file = await FindWindowByViewModel(this).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Select location for new Diablo pack image for drive {driveNumber}",
            ShowOverwritePrompt = true,
            SuggestedFileName = "NewPack.dsk",
            FileTypeChoices = new FilePickerFileType[]
            {
                new("Diablo Disks")
                {
                    Patterns = new[] { "*.dsk;*.dsk44" },
                },
                new("Diablo 30")
                {
                    Patterns = new[] { "*.dsk" },
                },
                new("Diablo 44")
                {
                    Patterns = new[] { "*.dsk44" },
                },
                FilePickerFileTypes.All
            }
        });

        if (file == null)
        {
            return;
        }

        try
        {
            string imagePath = file.Path.AbsolutePath;
            _system.LoadDiabloDrive(driveNumber, imagePath, true);

            if (driveNumber == 0)
            {
                _system.Configuration.Drive0Image = imagePath;
            }
            else
            {
                _system.Configuration.Drive1Image = imagePath;
            }

            OnPropertyChanged(nameof(DiabloDriveNames));
        }
        catch 
        { 
            // TODO: AvaloniaUI doesn't provide a MessageBox so... uh, we'll just eat this for now?
        }
    }

    public ICommand LoadTridentDrive { get; }

    private async void OnLoadTridentDrive(int driveNumber)
    {
        // Start async operation to open the dialog.
        var files = await FindWindowByViewModel(this).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"Load pack for Trident drive {driveNumber}",
            AllowMultiple = false,
            FileTypeFilter = new FilePickerFileType[]
            {
                new("Trident Disks")
                {
                    Patterns = new[] { "*.dsk80;*.dsk300" },
                },
                new("Trident T80")
                {
                    Patterns = new[] { "*.dsk80" },
                },
                new("Trident T300")
                {
                    Patterns = new[] { "*.dsk300" },
                },
                FilePickerFileTypes.All
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        string imagePath = files.First().Path.AbsolutePath;
        _system.LoadTridentDrive(driveNumber, imagePath, false);
        _system.Configuration.TridentImages[driveNumber] = imagePath;

        OnPropertyChanged(nameof(TridentDriveNames));
    }

    public ICommand UnloadTridentDrive { get; }

    private void OnUnloadTridentDrive(int driveNumber)
    {
        _system.UnloadDiabloDrive(driveNumber);
        _system.Configuration.TridentImages[driveNumber] = null;
        OnPropertyChanged(nameof(DiabloDriveNames));
    }

    public ICommand NewTridentDrive { get; }

    private async void OnNewTridentDrive(int driveNumber)
    {
        var file = await FindWindowByViewModel(this).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Select location for new Trident pack image for drive {driveNumber}",
            ShowOverwritePrompt = true,
            SuggestedFileName = "NewPack.dsk80",
            FileTypeChoices = new FilePickerFileType[]
            {
                new("Trident Disks")
                {
                    Patterns = new[] { "*.dsk80;*.dsk300" },
                },
                new("Trident T80")
                {
                    Patterns = new[] { "*.dsk80" },
                },
                new("Trident T300")
                {
                    Patterns = new[] { "*.dsk300" },
                },
                FilePickerFileTypes.All
            }
        });

        if (file == null)
        {
            return;
        }

        try
        {
            string imagePath = file.Path.AbsolutePath;
            _system.LoadTridentDrive(driveNumber, imagePath, true);
            _system.Configuration.TridentImages[driveNumber] = imagePath;
            OnPropertyChanged(nameof(TridentDriveNames));
        }
        catch
        {
            // TODO: AvaloniaUI doesn't provide a MessageBox so... uh, we'll just eat this for now?
        }
    }

    public ICommand ShowSystemConfigurationDialog { get; }

    private async void OnShowSystemConfigurationDialog()
    {
        ConfigurationViewModel vm = new ConfigurationViewModel(_system);
        ConfigurationDialog configurationDialog = new ConfigurationDialog()
        {
            DataContext = vm
        };

        await configurationDialog.ShowDialog(FindWindowByViewModel(this));

        // Invalidate display properties that may have been affected by the config change
        OnPropertyChanged(nameof(CompensatedWidth));
        OnPropertyChanged(nameof(CompensatedHeight));
        OnPropertyChanged(nameof(DisplayBitmap));
    }

    public ICommand ShowAlternateBootDialog { get; }

    private async void OnShowAlternateBootDialog()
    {
        ConfigurationViewModel vm = new ConfigurationViewModel(_system);
        AlternateBootDialog bootDialog = new AlternateBootDialog()
        {
            DataContext = vm
        };

        await bootDialog.ShowDialog(FindWindowByViewModel(this));
    }

    public ICommand ShowDebuggerWindow { get; }

    private void OnShowDebuggerWindow()
    {
        ConfigurationViewModel vm = new ConfigurationViewModel(_system);
        DebuggerWindow debuggerWindow = new DebuggerWindow()
        {
            DataContext = vm
        };

        debuggerWindow.Show(FindWindowByViewModel(this));
    }

    public ICommand ShowAboutDialog { get; }

    private async void OnShowAboutDialog()
    {
        ConfigurationViewModel vm = new ConfigurationViewModel(_system);
        AboutDialog aboutDialog = new AboutDialog()
        {
            DataContext = vm
        };

        await aboutDialog.ShowDialog(FindWindowByViewModel(this));
    }


    // KB/Mouse stuff
    public void OnKeyDown(KeyEventArgs e)
    {
        // Short-circuit if a script is playing.
        if (ScriptManager.IsPlaying)
        {
            return;
        }

        // Handle non-modifier keys here
        if (_keyMap.ContainsKey(e.Key))
        {
            _system.Keyboard.KeyDown(_keyMap[e.Key]);
        }

        if (_keysetMap.ContainsKey(e.Key))
        {
            _system.MouseAndKeyset.KeysetDown(_keysetMap[e.Key]);
        }
    }

    public void OnKeyUp(KeyEventArgs e)
    {
        // Short-circuit if a script is playing.
        if (ScriptManager.IsPlaying)
        {
            return;
        }

        // Handle non-modifier keys here
        if (_keyMap.ContainsKey(e.Key))
        {
            _system.Keyboard.KeyUp(_keyMap[e.Key]);
        }

        if (_keysetMap.ContainsKey(e.Key))
        {
            _system.MouseAndKeyset.KeysetUp(_keysetMap[e.Key]);
        }
    }

    public void OnMouseMoved(double scaledX, double scaledY)
    {
        // Short-circuit if a script is playing.
        if (ScriptManager.IsPlaying)
        {
            return;
        }

        // coordinates are the fraction across the screen the host's mouse is positioned
        _system.MouseAndKeyset.MouseMoveAbsolute((int)(scaledX * ALTO_DISPLAY_BITMAP_WIDTH), (int)(scaledY * ALTO_DISPLAY_HEIGHT));
    }

    public void OnMouseDown(bool left, bool middle, bool right)
    {
        // Short-circuit if a script is playing.
        if (ScriptManager.IsPlaying)
        {
            return;

        }
        AltoMouseButton buttons = (left ? AltoMouseButton.Left : 0) | (middle ? AltoMouseButton.Middle : 0) | (right ? AltoMouseButton.Right : 0);
        _system.MouseAndKeyset.MouseDown(buttons);
    }

    public void OnMouseUp(bool left, bool middle, bool right)
    {
        // Short-circuit if a script is playing.
        if (ScriptManager.IsPlaying)
        {
            return;
        }

        AltoMouseButton buttons = (left ? AltoMouseButton.Left : 0) | (middle ? AltoMouseButton.Middle : 0) | (right ? AltoMouseButton.Right : 0);
        _system.MouseAndKeyset.MouseUp(buttons);
    }

    // IAltoDisplay implementation
    // TODO: would be nice to reconcile out this 1bpp to 32bpp conversion process here and just use 32bpp everywhere.
    public void DrawDisplayWord(int scanline, int wordOffset, ushort word, bool lowRes)
    {
        if (lowRes)
        {
            // Low resolution; double up pixels.
            int address = scanline * 76 + wordOffset * 4;

            if (address > _1bppDisplayBuffer.Length)
            {
                throw new InvalidOperationException("Display word address is out of bounds.");
            }

            UInt32 stretched = StretchWord(word);

            _1bppDisplayBuffer[address] = (byte)(stretched >> 24);
            _1bppDisplayBuffer[address + 1] = (byte)(stretched >> 16);
            _1bppDisplayBuffer[address + 2] = (byte)(stretched >> 8);
            _1bppDisplayBuffer[address + 3] = (byte)(stretched);
        }
        else
        {
            int address = scanline * 76 + wordOffset * 2;

            if (address > _1bppDisplayBuffer.Length)
            {
                throw new InvalidOperationException("Display word address is out of bounds.");
            }

            _1bppDisplayBuffer[address] = (byte)(word >> 8);
            _1bppDisplayBuffer[address + 1] = (byte)(word);
        }

    }

    /// <summary>
    /// Invoked by the DisplayController to draw the cursor at the specified position on the given
    /// scanline.
    /// </summary>
    /// <param name="scanline">The scanline (Y)</param>
    /// <param name="xOffset">X offset (in pixels)</param>
    /// <param name="cursorWord">The word to be drawn</param>
    public void DrawCursorWord(int scanline, int xOffset, bool whiteOnBlack, ushort cursorWord)
    {

        int address = scanline * 76 + xOffset / 8;

        //
        // Grab the 32 bits straddling the cursor from the display buffer
        // so we can merge the 16 cursor bits in.
        //
        UInt32 displayWord = (UInt32)((_1bppDisplayBuffer[address] << 24) |
                                    (_1bppDisplayBuffer[address + 1] << 16) |
                                    (_1bppDisplayBuffer[address + 2] << 8) |
                                    _1bppDisplayBuffer[address + 3]);

        // Slide the cursor word to the proper X position
        UInt32 adjustedCursorWord = (UInt32)(cursorWord << (16 - (xOffset % 8)));

        if (!whiteOnBlack)
        {
            displayWord &= ~adjustedCursorWord;
        }
        else
        {
            displayWord |= adjustedCursorWord;
        }

        _1bppDisplayBuffer[address] = (byte)(displayWord >> 24);
        _1bppDisplayBuffer[address + 1] = (byte)(displayWord >> 16);
        _1bppDisplayBuffer[address + 2] = (byte)(displayWord >> 8);
        _1bppDisplayBuffer[address + 3] = (byte)(displayWord);

    }

    /// <summary>
    /// "Stretches" a 16 bit word into a 32-bit word (for low-res display purposes).
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    private UInt32 StretchWord(ushort word)
    {
        UInt32 stretched = 0;

        for (int i = 0x8000, j = 15; j >= 0; i = i >> 1, j--)
        {
            uint bit = (uint)(word & i) >> j;

            stretched |= (bit << (j * 2 + 1));
            stretched |= (bit << (j * 2));
        }

        return stretched;
    }

    /// <summary>
    /// Transmogrify 1bpp display buffer to 32-bits.
    /// TODO: can we just eliminate this
    /// </summary>
    private void ExpandBitmapToARGB()
    {
        int rgbIndex = 0;

        for (int i = 0; i < _32bppDisplayBuffer.Length / 8; i++)
        {
            byte b = _1bppDisplayBuffer[i];
            for (int bit = 7; bit >= 0; bit--)
            {
                if ((b & (1 << bit)) == 0)
                {
                    if (_system.Configuration.SlowPhosphorSimulation)
                    {
                        // Fade the pixel out via a total hack:
                        uint pixel = (uint)_32bppDisplayBuffer[rgbIndex];
                        int alpha = (int)((pixel & 0xff000000) >> 24);
                        alpha = Math.Max(0, alpha - 0x10);
                        _32bppDisplayBuffer[rgbIndex] = (int)((alpha << 24) | (pixel & 0x00ffffff));
                    }
                    else
                    {
                        // Just clobber it
                        _32bppDisplayBuffer[rgbIndex] = 0;
                    }
                }
                else
                {
                    // TODO: this is dumb; the compiler will not accept a cast of _litPixel to an int
                    // (the buffer has to be an "int" because *FOR SOME REASON* (it's visual basic)
                    // Marshal.Copy takes an array of ints.) but we can FOOL IT by doing this, and wow,
                    // it works!
                    uint c = _litPixelSlow;
                    _32bppDisplayBuffer[rgbIndex] = (int)c;
                }

                rgbIndex++;
            }
        }
    }

    public void Render()
    {
        ExpandBitmapToARGB();

        using (var frameBuffer = _currentBitmap.Lock())
        {
            Marshal.Copy(_32bppDisplayBuffer, 0, frameBuffer.Address, _32bppDisplayBuffer.Length);
        }

        // The double-buffering we're doing here makes the display more stable at higher framerates but the real reason
        // we're doing this is that modifying a WriteableBitmap doesn't invalidate the Image it's bound
        // to so we do this to invalidate the property and force it to update.

        // The below doesn't work but would be a nice way to scale w/out interpolation, should Avalonia/Skia ever fix the issue
        //Bitmap scaledBitmap = _currentBitmap.CreateScaledBitmap(new PixelSize(ALTO_DISPLAY_WIDTH * 2, ALTO_DISPLAY_HEIGHT * 2), BitmapInterpolationMode.None);
        DisplayBitmap = _currentBitmap;
        SwapBuffers();

        // Delay (if necessary) if we're speed-throttling execution
        if (_system.Configuration.ThrottleSpeed)
        {
            if (!_frameTimer.IsRunning)
            {
                _frameTimer.Start();
            }

            _frameTimer.Wait();
        }
        else
        {
            _frameTimer.Stop();
        }
    }

    private void SwapBuffers()
    {
        if (_currentBitmap == _displayBitmap1)
        {
            _currentBitmap = _displayBitmap0;
        }
        else
        {
            _currentBitmap = _displayBitmap1;
        }
    }


    private void InitKeymap()
    {
        _keyMap = new Dictionary<Key, AltoKey>();

        _keyMap.Add(Key.A, AltoKey.A);
        _keyMap.Add(Key.B, AltoKey.B);
        _keyMap.Add(Key.C, AltoKey.C);
        _keyMap.Add(Key.D, AltoKey.D);
        _keyMap.Add(Key.E, AltoKey.E);
        _keyMap.Add(Key.F, AltoKey.F);
        _keyMap.Add(Key.G, AltoKey.G);
        _keyMap.Add(Key.H, AltoKey.H);
        _keyMap.Add(Key.I, AltoKey.I);
        _keyMap.Add(Key.J, AltoKey.J);
        _keyMap.Add(Key.K, AltoKey.K);
        _keyMap.Add(Key.L, AltoKey.L);
        _keyMap.Add(Key.M, AltoKey.M);
        _keyMap.Add(Key.N, AltoKey.N);
        _keyMap.Add(Key.O, AltoKey.O);
        _keyMap.Add(Key.P, AltoKey.P);
        _keyMap.Add(Key.Q, AltoKey.Q);
        _keyMap.Add(Key.R, AltoKey.R);
        _keyMap.Add(Key.S, AltoKey.S);
        _keyMap.Add(Key.T, AltoKey.T);
        _keyMap.Add(Key.U, AltoKey.U);
        _keyMap.Add(Key.V, AltoKey.V);
        _keyMap.Add(Key.W, AltoKey.W);
        _keyMap.Add(Key.X, AltoKey.X);
        _keyMap.Add(Key.Y, AltoKey.Y);
        _keyMap.Add(Key.Z, AltoKey.Z);
        _keyMap.Add(Key.D0, AltoKey.D0);
        _keyMap.Add(Key.D1, AltoKey.D1);
        _keyMap.Add(Key.D2, AltoKey.D2);
        _keyMap.Add(Key.D3, AltoKey.D3);
        _keyMap.Add(Key.D4, AltoKey.D4);
        _keyMap.Add(Key.D5, AltoKey.D5);
        _keyMap.Add(Key.D6, AltoKey.D6);
        _keyMap.Add(Key.D7, AltoKey.D7);
        _keyMap.Add(Key.D8, AltoKey.D8);
        _keyMap.Add(Key.D9, AltoKey.D9);
        _keyMap.Add(Key.Space, AltoKey.Space);
        _keyMap.Add(Key.OemPeriod, AltoKey.Period);
        _keyMap.Add(Key.OemComma, AltoKey.Comma);
        _keyMap.Add(Key.OemQuotes, AltoKey.Quote);
        _keyMap.Add(Key.Oem5, AltoKey.BSlash);
        _keyMap.Add(Key.OemBackslash, AltoKey.BSlash);
        _keyMap.Add(Key.OemQuestion, AltoKey.FSlash);
        _keyMap.Add(Key.OemPlus, AltoKey.Plus);
        _keyMap.Add(Key.OemMinus, AltoKey.Minus);
        _keyMap.Add(Key.Escape, AltoKey.ESC);
        _keyMap.Add(Key.Delete, AltoKey.DEL);
        _keyMap.Add(Key.Left, AltoKey.Arrow);
        _keyMap.Add(Key.LeftShift, AltoKey.LShift);
        _keyMap.Add(Key.RightShift, AltoKey.RShift);
        _keyMap.Add(Key.LeftCtrl, AltoKey.CTRL);
        _keyMap.Add(Key.RightCtrl, AltoKey.CTRL);
        _keyMap.Add(Key.Return, AltoKey.Return);
        _keyMap.Add(Key.F1, AltoKey.BlankTop);
        _keyMap.Add(Key.F2, AltoKey.BlankMiddle);
        _keyMap.Add(Key.F3, AltoKey.BlankBottom);
        _keyMap.Add(Key.F4, AltoKey.Lock);
        _keyMap.Add(Key.Back, AltoKey.BS);
        _keyMap.Add(Key.Tab, AltoKey.TAB);
        _keyMap.Add(Key.OemSemicolon, AltoKey.Semicolon);
        _keyMap.Add(Key.OemOpenBrackets, AltoKey.LBracket);
        _keyMap.Add(Key.OemCloseBrackets, AltoKey.RBracket);
        _keyMap.Add(Key.Down, AltoKey.LF);

        _keysetMap = new Dictionary<Key, AltoKeysetKey>();

        // Map the 5 keyset keys to F5-F9.
        _keysetMap.Add(Key.F5, AltoKeysetKey.Keyset0);
        _keysetMap.Add(Key.F6, AltoKeysetKey.Keyset1);
        _keysetMap.Add(Key.F7, AltoKeysetKey.Keyset2);
        _keysetMap.Add(Key.F8, AltoKeysetKey.Keyset3);
        _keysetMap.Add(Key.F9, AltoKeysetKey.Keyset4);
    }

    private void OnUiTimer(object? state)
    {
        _fieldsRendered = _system.DisplayController.Fields;
        _system.DisplayController.Fields = 0;
        OnPropertyChanged(nameof(FieldsPerSecond));
    }


    // Alto System managed by the UI
    private AltoSystem _system;

    // Bitmap for Avalonia use
    private WriteableBitmap _displayBitmap0;
    private WriteableBitmap _displayBitmap1;
    private WriteableBitmap _currentBitmap;
    private WriteableBitmap _displayBitmap;
    private double _hostDesktopDisplayScale;

    // Timer for frame throttling
    private HighResolutionTimer _frameTimer;

    // Status-line information
    private Timer           _uiTimer;
    private int             _fieldsRendered;

    //
    // Buffer for display pixels.  This is 1bpp, directly written by the Alto.
    //
    private byte[] _1bppDisplayBuffer = new byte[ALTO_DISPLAY_HEIGHT * 76 + 4];        // + 4 (32-bits) to make cursor display logic simpler.
                                                                                       // and 608 pixels wide to make it a multiple of 8 bits.

    //
    // Buffer for rendering pixels.  SDL doesn't support 1bpp pixel formats, so to keep things simple we use
    // an array of ints and a 32bpp format.  What's a few extra bits between friends.
    //
    private int[] _32bppDisplayBuffer = new int[ALTO_DISPLAY_HEIGHT * ALTO_DISPLAY_BITMAP_WIDTH + 8];     // + 8 (32-bits) as above.
                                                                                                   // and 608 pixels wide as above.

    private const uint _litPixelSlow = 0xffdffcff;   // slightly bluish-greenish
    private const uint _offPixelSlow = 0x20000000;   // provides a fakey-phosphor persistence

    private const int ALTO_DISPLAY_BITMAP_WIDTH = 608;  // rounded up to 608 so it's a nice even multiple of 8 bits.
    private const int ALTO_DISPLAY_WIDTH = 606;
    private const int ALTO_DISPLAY_HEIGHT = 808;

    // Keyboard mapping from windows vkeys to Alto keys
    private Dictionary<Key, AltoKey> _keyMap;

    // Keyset mapping from windows vkeys to Keyset keys
    private Dictionary<Key, AltoKeysetKey> _keysetMap;
}
