/*

Copyright(c) 2016 - 2020 Living Computers: Museum + Labs
Copyright(c) 2016 - 2024 Josh Dersch

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

1.Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
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

using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Contralto;
using Contralto.CPU;
using Contralto.Logging;
using iText.StyledXmlParser.Jsoup.Helper;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ContraltoUI.ViewModels
{
    public class SourceLine : INotifyPropertyChanged
    {
        public SourceLine(string sourceText, int lineNumber)
        {
            //
            // Mangle "<-" found in the source into the unicode arrow character, just to be neat.
            //
            sourceText = sourceText.Replace("<-", _arrowChar.ToString());

            // See if line begins with something of the form "TNxxxxx>".
            // If it does then we have extra metadata to parse out.
            string[] tokens = sourceText.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool annotated = false;

            LineNumber = lineNumber;

            if (tokens.Length > 0 &&
                tokens[0].Length == 7 &&
                tokens[0].EndsWith(">"))
            {
                // Close enough.  Look for the task tag and parse out the (octal) address
                switch (tokens[0].Substring(0, 2))
                {
                    case "EM":
                        Task = TaskType.Emulator;
                        break;

                    case "OR":
                        Task = TaskType.Orbit;
                        break;

                    case "SE":
                        Task = TaskType.DiskSector;
                        break;

                    case "EN":
                        Task = TaskType.Ethernet;
                        break;

                    case "MR":
                        Task = TaskType.MemoryRefresh;
                        break;

                    case "DW":
                        Task = TaskType.DisplayWord;
                        break;

                    case "CU":
                        Task = TaskType.Cursor;
                        break;

                    case "DH":
                        Task = TaskType.DisplayHorizontal;
                        break;

                    case "DV":
                        Task = TaskType.DisplayVertical;
                        break;

                    case "PA":
                        Task = TaskType.Parity;
                        break;

                    case "KW":
                        Task = TaskType.DiskWord;
                        break;

                    case "XM":  //XMesa code, which runs in the Emulator task
                        Task = TaskType.Emulator;
                        break;

                    default:
                        Task = TaskType.Invalid;
                        break;
                }

                if (Task != TaskType.Invalid)
                {
                    try
                    {
                        // Belongs to a task, so we can grab the address out as well
                        string addressText = sourceText.Substring(2, 4);
                        Address = Convert.ToUInt16(addressText, 8);
                        annotated = true;
                    }
                    catch
                    {
                        // That didn't work for whatever reason, just treat this as a normal source line.
                        annotated = false;
                    }

                    string sourceCode = sourceText.Substring(tokens[0].Length, sourceText.Length - tokens[0].Length);
                    // Remove single leading space if present
                    if (sourceCode.StartsWith(" "))
                    {
                        sourceCode = sourceCode.Substring(1);
                    }

                    Text = UnTabify(sourceCode);
                }
                else
                {
                    // We will just display this as a non-source line
                    annotated = false;
                }
            }

            if (!annotated)
            {
                Text = UnTabify(sourceText);
                Address = 0;
                Task = TaskType.Invalid;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Text { get; }
        public ushort Address { get; }
        public TaskType Task { get; }

        public bool Breakpoint
        {
            get { return _breakPoint; }
            set
            {
                if (value != _breakPoint)
                {
                    _breakPoint = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(Breakpoint)));
                    }
                }
            }
        }
        public int LineNumber { get;  }

        /// <summary>
        /// Must override this to make the Avalonia DataGrid control happy, otherwise
        /// it gets extremely confused, for some reason.  Two lines are equivalent
        /// only if they're literally the same source line.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null || !(obj is SourceLine))
            {
                return false;
            }

            SourceLine line = (SourceLine)obj;

            return (line.LineNumber == this.LineNumber && line.Text == this.Text);
        }

        /// <summary>
        /// Converts tabs in the given string to 8 space tabulation.  As it should be.
        /// </summary>
        /// <param name="tabified"></param>
        /// <returns></returns>
        private string UnTabify(string tabified)
        {
            StringBuilder untabified = new StringBuilder();

            int column = 0;

            foreach (char c in tabified)
            {
                if (c == '\t')
                {
                    untabified.Append(" ");
                    column++;
                    while ((column % 8) != 0)
                    {
                        untabified.Append(" ");
                        column++;
                    }
                }
                else
                {
                    untabified.Append(c);
                    column++;
                }
            }

            return untabified.ToString();
        }

        // Unicode character for the Arrow used by Alto microcode
        private const char _arrowChar = (char)0x2190;

        private bool _breakPoint;

        public static bool operator ==(SourceLine left, SourceLine right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SourceLine left, SourceLine right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode() ^ Address;
        }
    }

    public partial class DebuggerViewModel : ViewModelBase
    {
        public DebuggerViewModel(AltoSystem system)
        {
            _system = system;

            _system.Controller.StepCallback += OnExecutionStep;
            _system.Controller.ErrorCallback += OnExecutionError;

            // Pick up the current execution status (if the main window hands us a running
            // system, we want to know).
            _execType = _system.Controller.IsRunning ? ExecutionType.Normal : ExecutionType.None;

            _microcodeSource = LoadSourceCode(Path.Combine("Disassembly", "altoIIcode3.mu"));
            _xmesaSource = LoadSourceCode(Path.Combine("Disassembly", "MesaROM.mu"));

            _microcodeSource.ListChanged += _microcodeSource_ListChanged;

            OnPropertyChanged(nameof(MicrocodeSource));
            OnPropertyChanged(nameof(MesaSource));

            _destinationAddress = 0;
            _currentSourceLineIndex = 0;

            JumpToAddress = ReactiveCommand.Create(OnJumpToAddress);
        }

        private void _microcodeSource_ListChanged(object? sender, ListChangedEventArgs e)
        {
            // TODO: rename, have this keep the list of active breakpoints up-to-date.
            Log.Write(LogComponent.All, "WTF");
        }

        public ICommand JumpToAddress { get; }

        private void OnJumpToAddress()
        {
            SourceLine? line = _microcodeSource.Where(l => l.Address == _destinationAddress).FirstOrDefault();
            if (line == null)
            {
                return;
            }

            CurrentSourceLine = line;
            CurrentSourceLineIndex = line.LineNumber;

            OnPropertyChanged(nameof(CurrentSourceLine));
        }


        public override void OnApplicationExit()
        {

        }

        public BindingList<SourceLine> MicrocodeSource
        {
            get { return _microcodeSource; }
        }
        public BindingList<SourceLine> MesaSource => _xmesaSource;

        public SourceLine CurrentSourceLine { get; set; }

        public int CurrentSourceLineIndex
        {
            get { return _currentSourceLineIndex; }
            set
            {
                if (value == _currentSourceLineIndex)
                {
                    return;
                }

                _currentSourceLineIndex = value;
                OnPropertyChanged(nameof(CurrentSourceLineIndex));
            }
        }

        public string DestinationAddress
        {
            get { return Conversion.ToOctal(_destinationAddress); }
            set
            {
                try
                {
                    ushort newAddress = Convert.ToUInt16(value, 8);

                    if (newAddress >= 0x8000)
                    {
                        throw new ArgumentOutOfRangeException(nameof(newAddress));
                    }

                    _destinationAddress = newAddress;
                }
                catch
                {
                    throw new DataValidationException("Value must be specified in octal and be between 0 and 1777.");
                }
                OnPropertyChanged(nameof(DestinationAddress));
            }
        }

        private BindingList<SourceLine> LoadSourceCode(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(path, "Microcode path must be specified.");
            }

            BindingList<SourceLine> lines = new BindingList<SourceLine>();

            using (StreamReader sr = new StreamReader(path))
            {
                int lineNumber = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    lines.Add(new SourceLine(line, lineNumber++));
                }
            }

            return lines;
        }

        private string GetTextForTaskState(AltoCPU.Task task)
        {
            if (task == null)
            {
                return String.Empty;
            }
            else
            {
                // Wakeup bit
                string status = task.Wakeup ? "W" : String.Empty;

                // Run bit
                if (task.TaskType == _system.CPU.CurrentTask.TaskType)
                {
                    status += "R";
                }

                return status;
            }
        }

        private string GetTextForTaskBank(AltoCPU.Task task)
        {
            if (task == null)
            {
                return String.Empty;
            }
            else
            {
                return _system.CPU.UCodeMemory.GetBank(task.TaskType).ToString();
            }
        }

        public static string GetTextForTask(TaskType task)
        {

            if (task == TaskType.Invalid)
            {
                return String.Empty;
            }
            else
            {
                return _taskText[(int)task];
            }
        }

        private static string[] _taskText =
            {
                "EM",   // 0 - emulator
                "OR",   // 1 - orbit
                String.Empty,
                "TO",   // 3 - trident output
                "KS",   // 4 - disk sector
                String.Empty,
                String.Empty,
                "EN",   // 7 - ethernet
                "MR",   // 8 - memory refresh
                "DW",   // 9 - display word
                "CU",   // 10 - cursor
                "DH",   // 11 - display horizontal
                "DV",   // 12 - display vertical
                "PA",   // 13 - parity
                "KW",   // 14 - disk word
                "TI",   // 15 - trident input
            };

        /// <summary>
        /// TODO: this likely belongs elsewhere
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static IImmutableSolidColorBrush GetColorForTask(TaskType task)
        {
            bool isDarkMode = Application.Current.ActualThemeVariant == ThemeVariant.Dark;

            if (task == TaskType.Invalid)
            {
                return Brushes.Transparent;
            }
            else
            {
                return isDarkMode ? _taskColorsDark[(int)task] : _taskColorsLight[(int)task];
            }
        }

        private static IImmutableSolidColorBrush[] _taskColorsLight =
            {
                Brushes.LightBlue,    // 0 - emulator
                Brushes.LightGoldenrodYellow,    // 1 - orbit
                Brushes.LightGray,    // 2 - unused
                Brushes.LightCoral,   // 3 - trident output
                Brushes.LightGreen,   // 4 - disk sector
                Brushes.LightGray,    // 5 - unused
                Brushes.LightGray,    // 6 - unused
                Brushes.LightSalmon,  // 7 - ethernet
                Brushes.LightSeaGreen,// 8 - memory refresh
                Brushes.LightYellow,  // 9 - display word
                Brushes.LightPink,    // 10 - cursor
                Brushes.Chartreuse,   // 11 - display horizontal
                Brushes.LightCoral,   // 12 - display vertical
                Brushes.LightSteelBlue, // 13 - parity
                Brushes.Gray,         // 14 - disk word
                Brushes.LightSteelBlue, // 15 - trident output
            };

        private static IImmutableSolidColorBrush[] _taskColorsDark =
            {
                Brushes.DarkBlue,    // 0 - emulator
                Brushes.DarkOrange,    // 1 - orbit
                Brushes.DarkGray,    // 2 - unused
                Brushes.DarkRed,   // 3 - trident output
                Brushes.DarkGreen,   // 4 - disk sector
                Brushes.DarkGray,    // 5 - unused
                Brushes.DarkGray,    // 6 - unused
                Brushes.DarkOrchid,  // 7 - ethernet
                Brushes.DarkSlateBlue,// 8 - memory refresh
                Brushes.DarkGoldenrod,  // 9 - display word
                Brushes.DarkMagenta,    // 10 - cursor
                Brushes.DarkOliveGreen,   // 11 - display horizontal
                Brushes.DarkSlateGray,   // 12 - display vertical
                Brushes.DarkTurquoise, // 13 - parity
                Brushes.Gray,         // 14 - disk word
                Brushes.DarkSlateGray, // 15 - trident output
            };


        private bool OnExecutionStep()
        {
            /*
            switch (_execType)
            {
                case ExecutionType.Auto:
                    {
                        // Execute a single step, then update UI and 
                        // sleep to give messages time to run.
                        //this.BeginInvoke(new StepDelegate(RefreshUI));
                        //this.BeginInvoke(new StepDelegate(Invalidate));
                        System.Threading.Thread.Sleep(10);
                        return false; // break always
                    }

                case ExecutionType.Step:
                    return true;  // break always

                case ExecutionType.Normal:
                case ExecutionType.NextTask:
                case ExecutionType.NextNovaInstruction:
                    // See if we need to stop here
                    if (_execAbort ||                                               // The Stop button was hit
                        _system.CPU.InternalBreak ||                                // Something internal has requested a debugger break
                        _microcodeBreakpointEnabled[
                            (int)UCodeMemory.GetBank(
                                _system.CPU.CurrentTask.TaskType),
                                _system.CPU.CurrentTask.MPC] ||                     // A microcode breakpoint was hit
                        (_execType == ExecutionType.NextTask &&
                            _system.CPU.NextTask != null &&
                            _system.CPU.NextTask != _system.CPU.CurrentTask) ||     // The next task was switched to                    
                        (_system.CPU.CurrentTask.MPC == 0x10 &&                     // MPC is 20(octal) meaning a new Nova instruction and...
                            (_novaBreakpointEnabled[_system.CPU.R[6]] ||            // A breakpoint is set here
                             _execType == ExecutionType.NextNovaInstruction)))      // or we're running only a single Nova instruction.                              
                    {
                        if (!_execAbort)
                        {
                            SetExecutionState(ExecutionState.BreakpointStop);
                        }

                        // Stop here as we've hit a breakpoint or have been stopped 
                        // Update UI to indicate where we stopped.
                        this.BeginInvoke(new StepDelegate(RefreshUI));
                        this.BeginInvoke(new StepDelegate(Invalidate));

                        _system.CPU.InternalBreak = false;

                        _execAbort = false;
                        return true;
                    }

                    break;
            } */

            return false;
        }

        private void OnExecutionError(Exception e)
        {

            _lastExceptionText = e.Message;
            // SetExecutionState(ExecutionState.InternalError);
        }

        private enum ExecutionType
        {
            None = 0,
            Step,
            Auto,
            Normal,
            NextTask,
            NextNovaInstruction,
        }

        private enum ExecutionState
        {
            Stopped = 0,
            SingleStep,
            AutoStep,
            Running,
            BreakpointStop,
            InternalError,
        }

        // Source listings
        private BindingList<SourceLine> _microcodeSource;
        private BindingList<SourceLine> _xmesaSource;

        private int _currentSourceLineIndex;

        // Address to jump to
        private ushort _destinationAddress;

        // Execution / error state
        private bool _execAbort;
        private ExecutionState _execState;
        private ExecutionType _execType;
        private string _lastExceptionText;

        private delegate void StepDelegate();

        private AltoSystem _system;
    }
}
