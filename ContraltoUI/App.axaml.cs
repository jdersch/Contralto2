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

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Contralto;
using ContraltoUI.ViewModels;
using ContraltoUI.Views;
using System;

namespace ContraltoUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        IClassicDesktopStyleApplicationLifetime? desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        if (desktop == null)
        {
            return;
        }

        ParseArgs(desktop.Args);

        // TODO: move this inside AltoUIViewModel entirely?
        Configuration config = new Configuration(true);
        _system = new AltoSystem(config);

        if (config.EnableAudioDAC)
        {
            SDLAudioSink audioSink = new SDLAudioSink();
            _system.AudioDAC.AttachSink(audioSink);
        }

        AltoUIViewModel vm = new AltoUIViewModel(_system);

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        desktop.MainWindow = new MainWindow()
        {
            DataContext = vm
        };


        base.OnFrameworkInitializationCompleted();
    }

    private void ParseArgs(string[]? args)
    {
        if (args == null || args.Length == 0)
        {
            return;
        }

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i++].ToLowerInvariant())
            {
                case "-config":
                    if (i < args.Length)
                    {
                        StartupOptions.ConfigurationFile = args[i];
                    }
                    else
                    {
                        PrintUsage();
                        return;
                    }
                    break;

                case "-script":
                    if (i < args.Length)
                    {
                        StartupOptions.ScriptFile = args[i];
                    }
                    else
                    {
                        PrintUsage();
                        return;
                    }
                    break;

                case "-rompath":
                    if (i < args.Length)
                    {
                        StartupOptions.RomPath = args[i];
                    }
                    else
                    {
                        PrintUsage();
                        return;
                    }
                    break;

                default:
                    PrintUsage();
                    return;
            }
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: ContrAlto [-config <configurationFile>] [-script <scriptFile>]");
    }

    private AltoSystem _system = null!;
}
