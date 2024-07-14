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

namespace Contralto.Scripting
{
    public class ControlCommands
    {
        public ControlCommands(AltoSystem system)
        {
            _system = system;
        }

        [DebuggerFunction("quit", "Exits ContrAlto.")]
        private CommandResult Quit()
        {
            _system.Controller.StopExecution();
            return CommandResult.Quit;
        }

        [DebuggerFunction("quit without saving", "Exits ContrAlto without committing changes to Diablo disk packs.")]
        private CommandResult QuitNoSave()
        {
            _system.Controller.StopExecution();
            return CommandResult.QuitNoSave;
        }

        [DebuggerFunction("start", "Starts the emulated Alto normally.")]
        private CommandResult Start()
        {
            if (_system.Controller.IsRunning)
            {
                Console.WriteLine("Alto is already running.");
            }
            else
            {
                _system.Controller.StartExecution(AlternateBootType.None);
                Console.WriteLine("Alto started.");
            }

            return CommandResult.Normal;
        }

        [DebuggerFunction("stop", "Stops the emulated Alto.")]
        private CommandResult Stop()
        {
            _system.Controller.StopExecution();
            Console.WriteLine("Alto stopped.");

            return CommandResult.Normal;
        }

        [DebuggerFunction("reset", "Resets the emulated Alto.")]
        private CommandResult Reset()
        {
            _system.Controller.Reset(AlternateBootType.None);
            Console.WriteLine("Alto reset.");

            return CommandResult.Normal;
        }

        [DebuggerFunction("start with keyboard disk boot", "Starts the emulated Alto with the specified keyboard disk boot address.")]
        private CommandResult StartDisk()
        {
            if (_system.Controller.IsRunning)
            {
                _system.Controller.Reset(AlternateBootType.Disk);
            }
            else
            {
                _system.Controller.StartExecution(AlternateBootType.Disk);
            }

            return CommandResult.Normal;
        }

        [DebuggerFunction("start with keyboard net boot", "Starts the emulated Alto with the specified keyboard ethernet boot number.")]
        private CommandResult StartNet()
        {
            if (_system.Controller.IsRunning)
            {
                _system.Controller.Reset(AlternateBootType.Ethernet);
            }
            else
            {
                _system.Controller.StartExecution(AlternateBootType.Ethernet);
            }

            return CommandResult.Normal;
        }

        [DebuggerFunction("load disk", "Loads the specified drive with the requested disk image.", "<drive> <path>")]
        private CommandResult LoadDisk(ushort drive, string path)
        {
            if (drive > 1)
            {
                throw new InvalidOperationException("Drive specification out of range.");
            }

            // Load the new pack.
            _system.LoadDiabloDrive(drive, path, false);
            Console.WriteLine("Drive {0} loaded.", drive);

            return CommandResult.Normal;
        }

        [DebuggerFunction("unload disk", "Unloads the specified drive.", "<drive>")]
        private CommandResult UnloadDisk(ushort drive)
        {
            if (drive > 1)
            {
                throw new InvalidOperationException("Drive specification out of range.");
            }

            // Unload the current pack.
            _system.UnloadDiabloDrive(drive);
            Console.WriteLine("Drive {0} unloaded.", drive);

            return CommandResult.Normal;
        }

        [DebuggerFunction("new disk", "Creates and loads a new image for the specified drive.", "<drive>")]
        private CommandResult NewDisk(ushort drive, string path)
        {
            if (drive > 1)
            {
                throw new InvalidOperationException("Drive specification out of range.");
            }

            // Unload the current pack.
            _system.LoadDiabloDrive(drive, path, true);
            Console.WriteLine("Drive {0} created and loaded.", drive);

            return CommandResult.Normal;
        }        

        [DebuggerFunction("load trident", "Loads the specified trident drive with the requested disk image.", "<drive> <path>")]
        private CommandResult LoadTrident(ushort drive, string path)
        {
            if (drive > 7)
            {
                throw new InvalidOperationException("Drive specification out of range.");
            }

            // Load the new pack.
            _system.LoadTridentDrive(drive, path, false);
            Console.WriteLine("Trident {0} loaded.", drive);

            return CommandResult.Normal;
        }

        [DebuggerFunction("unload trident", "Unloads the specified trident drive.", "<drive>")]
        private CommandResult UnloadTrident(ushort drive)
        {
            if (drive > 7)
            {
                throw new InvalidOperationException("Drive specification out of range.");
            }

            // Unload the current pack.
            _system.UnloadTridentDrive(drive);
            Console.WriteLine("Trident {0} unloaded.", drive);

            return CommandResult.Normal;
        }

        [DebuggerFunction("new trident", "Creates and loads a new image for the specified drive.", "<drive>")]
        private CommandResult NewTrident(ushort drive, string path)
        {
            if (drive > 7)
            {
                throw new InvalidOperationException("Drive specification out of range.");
            }

            // Unload the current pack.
            _system.LoadTridentDrive(drive, path, true);
            Console.WriteLine("Trident {0} created and loaded.", drive);

            return CommandResult.Normal;
        }

        [DebuggerFunction("set ethernet address", "Sets the Alto's host Ethernet address.")]
        private CommandResult SetEthernetAddress(byte address)
        {
            if (address == 0 || address == 0xff)
            {
                Console.WriteLine("Address {0} is invalid.", Conversion.ToOctal(address));
            }
            else
            {
                _system.Configuration.HostAddress = address;
            }

            return CommandResult.Normal;
        }        

        [DebuggerFunction("set keyboard net boot file", "Sets the boot file used for net booting.")]
        private CommandResult SetKeyboardBootFile(ushort file)
        {
            _system.Configuration.BootFile = file;
            return CommandResult.Normal;
        }

        [DebuggerFunction("set keyboard disk boot address", "Sets the boot address used for disk booting.")]
        private CommandResult SetKeyboardBootAddress(ushort address)
        {
            _system.Configuration.BootFile = address;
            return CommandResult.Normal;
        }


        private AltoSystem _system;
    }
}
