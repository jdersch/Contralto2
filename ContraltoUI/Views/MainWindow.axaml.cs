﻿/*

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

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ContraltoUI.ViewModels;

namespace ContraltoUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is AltoUIViewModel vm)
        {
            // Pass along the scale factor to the viewmodel so we can compensate.
            vm.HostDesktopDisplayScale = DesktopScaling;
        }

        base.OnLoaded(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Is this tight coupling?  Is this private action?  Do I care?
        // (If there's a better way to do this, I'm unaware of it.)
        // Anyway -- let our viewmodel know what's going on so it can stop things.
        if (DataContext is ViewModelBase vm)
        {
            vm.OnApplicationExit();
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (DataContext is AltoUIViewModel vm)
        {
            vm.OnLostFocus();
        }
        base.OnLostFocus(e);
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        if (DataContext is AltoUIViewModel vm)
        {
            vm.OnFocused();
        }
        base.OnGotFocus(e);
    }


}
