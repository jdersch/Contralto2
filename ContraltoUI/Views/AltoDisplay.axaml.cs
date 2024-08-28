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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ContraltoUI.ViewModels;
using Org.BouncyCastle.Crypto.Agreement;

namespace ContraltoUI.Views;

public partial class AltoDisplay : UserControl
{
    public AltoDisplay()
    {
        InitializeComponent();

        // TODO: maybe make this a single pixel or something to make it visible if the Alto is displaying
        // no cursor at all?
        Bitmap cursorBitmap = new Bitmap(AssetLoader.Open(new System.Uri("avares://ContraltoUI/Assets/DisplayCursor.png")));
        _hiddenCursor = new Cursor(cursorBitmap, new PixelPoint(2,2));
    }

    // There may be some maaaagical databinding way I can do the below, I just don't care right now.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        AltoUIViewModel? alto = DataContext as AltoUIViewModel;
        alto?.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        AltoUIViewModel? alto = DataContext as AltoUIViewModel;
        alto?.OnKeyUp(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        // Send mouse coordinate normalized to the image's (DPI-compensated) render size.
        Point pointerPoint = e.GetPosition(DisplayImage);

        AltoUIViewModel? alto = DataContext as AltoUIViewModel;
        alto?.OnMouseMoved(pointerPoint.X / (DisplayImage.DesiredSize.Width / alto.HostDesktopDisplayScale), pointerPoint.Y / (DisplayImage.DesiredSize.Height / alto.HostDesktopDisplayScale));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        AltoUIViewModel? alto = DataContext as AltoUIViewModel;
        PointerPointProperties pointerPoint = e.GetCurrentPoint(this).Properties;
        alto?.OnMouseDown(pointerPoint.IsLeftButtonPressed, pointerPoint.IsMiddleButtonPressed, pointerPoint.IsRightButtonPressed);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        AltoUIViewModel? alto = DataContext as AltoUIViewModel;
        // this Avalonia API is weirdly asymmetrical.
        alto?.OnMouseUp(e.InitialPressMouseButton == MouseButton.Left, e.InitialPressMouseButton == MouseButton.Middle, e.InitialPressMouseButton == MouseButton.Right);
    }

    private Cursor _hiddenCursor;
}
