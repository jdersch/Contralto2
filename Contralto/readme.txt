﻿Readme.txt for Contralto v2.0.0beta:

1. Introduction and Overview
============================

ContrAlto aspires to be a faithful emulation of the Xerox Alto series of 
pioneering graphical workstations developed at Xerox PARC starting in 1973.

At this time, it runs all known software, though some may make use of hardware
that is not yet implemented (RS232, IMP interfaces, etc.)

This document covers installation and use of this emulator.

1.1 What's Emulated
-------------------

ContrAlto currently emulates the following Alto hardware:
   - Alto I and Alto II XM CPU
   - Microcode RAM (in 1K RAM, 1K RAM/2K ROM, or 3K RAM configurations)
   - 256KW of main memory (in 64KW banks)
   - Two Diablo Model 31 or 44 drives
   - Ethernet (encapsulated in either UDP datagrams or raw Ethernet frames
     on the host machine)
   - Standard Keyboard/Mouse/Video
   - Alto Keyset (5-key chording keyboard)
   - Audio DAC (used with the Smalltalk Music System)
   - The Orbit raster hardware, Dover Raster Output Scanner and Dover print
     engine, which provides 384dpi print output (currently PDF only)
   - The Trident Disk Controller (TriCon) and up to eight T-80 or T-300 
     drives

1.2 What's Not
--------------

At this time, ContrAlto does not support more exotic hardware such as the Diablo
printer, serial and IMP interfaces, or audio using the utility port.


2.0 Requirements
================

ContrAlto will run on any system capable of running version 8.0 or later of the 
Microsoft .NET Runtime.  If it is not installed on your computer instructions 
for getting it can be obtained at https://dotnet.microsoft.com/en-us/download/dotnet/8.0.

A three-button mouse is essential for using some Alto software.  On most mice,
the mousewheel can be clicked to provide the third (middle) button.  Laptops
with trackpads may have configuration options to simulate three buttons but
will likely be clumsy to use.


3.0 Getting Started
===================

Installation of ContrAlto is simple:  Unzip the release archive to a directory
of your choosing.  

To run the emulator, execute Contralto.exe (double-click in
explorer on Windows; run "dotnet Contralto.exe" in a shell on *nix and mac.)


3.1 Starting the Alto
=====================

On a real Alto, the system is booted by loading a 14" disk pack into the front
of a Diablo 31 drive, waiting for it to spin up for 20 seconds and then
pressing the "Reset" button on the back of the keyboard.

Booting an emulated Alto under ContrAlto is slightly less time-consuming.
To load a disk pack into the virtual Diablo drive, click on the "File"
menu and go to "Diablo Drive 0 -> Load...".  You will be presented with a file 
dialog allowing selection of the disk image (effectively a "virtual disk pack") 
to be loaded.  Disk images are included with ContrAlto in the "Disks" directory
and may also be found in various places on the Internet -- see Section 3.1.3 for 
details.

Once the pack has been loaded, you can start the Alto by clicking on the 
"System->Start" menu (or hitting Ctrl+Alt+S).  The display will turn white and
after 5-10 seconds a mouse cursor will appear, followed shortly by the banner
of the Xerox Alto Executive.  Congratulations, your Alto is now running!  Click
on the display window to start interacting with it using the keyboard and mouse.

3.1 Using the Alto
==================

3.1.1 Mouse
-----------

The Alto mouse is a three-button mouse.  Alto mouse buttons are mapped as you
would expect.  If you have a real three-button mouse then this is completely
straightforward.  If you have a two button mouse with a "mousewheel" then
a mousewheel click maps to a click of the Alto's middle mouse button.

If you have a trackpad or other pointing device, using the middle mouse button 
may be more complicated.  See what configuration options your operating system 
and/or drivers provide you for mapping mouse buttons or gestures.


3.1.2 Keyboard
--------------

ContrAlto emulates the 61-key Alto II keyboard.  The vast majority of keys
(the alphanumerics and punctuation) work as you would expect them to, but the
Alto has a few special keys, which are described below:

Alto Key       PC Key
--------       ----------
LF             Down Arrow
BS             Backspace
Blank-Top      F1
Blank-Middle   F2
Blank-Bottom   F3
<- (arrow)     Left Arrow
DEL            Del
LOCK           F4


3.1.3 Keyset
------------

A 5-key chording keyboard referred to as the "keyset" was a standard peripheral
in the early days of the Alto.  (It never caught on.)  The 5 keys on the keyset
are mapped to F5-F9 on your keyboard, with F5 corresponding to the leftmost key
and F9 corresponding to the rightmost.


3.1.4 Diablo Disk Packs
-----------------------

A real Alto uses large 14" disk packs for disk storage, each containing
approximately 2.5 megabytes (for Diablo 31) or 5 megabytes (for Diablo 44) of 
data.  ContrAlto uses files, referred to as "disk images" or just "images" 
that contain a bit-for-bit copy of these original packs.  These are a lot 
easier to use with a modern PC.

Disk images can be loaded, unloaded and created via the "File->Diablo Drive 0" 
and "File->Diablo Drive 1" menus.  A file dialog will be presented showing 
possible disk images in the current directory.

If you modify the contents of a loaded disk (for example creating new files or
deleting existing ones) the changes will be written back out to the disk image
when a new image is loaded or when ContrAlto exits.  For this reason it may be
a good idea to make backups of packs from time to time (just like on the real
machine.)

ContrAlto comes with a set of curated disk images:

TODO TODO TODO


Additionally and an assortment of Alto programs can be found on Bitsavers.org, at 
http://www.bitsavers.org/bits/Xerox/Alto/disk_images/.  Images include:

AllGames.dsk    -  A collection of games and toys for the Alto
chm/Bcpl.dsk    -  A set of BCPL development tools
chm/Diags.dsk   -  Diagnostic tools
chm/Bravox.dsk	-  The BravoX word processing environment
chm/Xmsmall.dsk -  Smalltalk-76


3.1.5 Trident Disk Packs
------------------------

Some Altos were used as file or print servers and needed greater storage 
capacity than the Diablo drives could provide.  These Altos used a special 
controller, referred to as the Trident (or TriCon) which could control up to
eight Century (later Xerox) T-80 or T-300 drives with a capacity of 80 or
300 megabytes, respectively.

ContrAlto can emulate a Trident controller and up to eight T-80 or T-300 drives
(in any combination.)  Like the Diablo, the contents of these disk packs are 
stored in image files.  These are loaded, unloaded, or created using the 
File->Trident Drives->Drive <N> menus.


3.1.6 Startup, Reset and Shutdown
---------------------------------

The system can be started at any time by using the "System->Start" menu, though
in general having a pack image loaded first is a good idea.  Similarly, the
"Start->Reset" menu will reset the Alto.

You can shut down the Alto by closing the ContrAlto window; this will commit
disk changes made to the currently loaded disks back to the disk image files.
However, you will want to be sure the software running on the Alto is ready
to be shutdown first, or else you may lose work or corrupt your disk.


3.2 Additional Reading Materials
----------------------------------

The Bitsavers Alto archive at http://http://bitsavers.org/pdf/xerox/alto is an 
excellent repository of original Alto documentation, here are a few documents to 
get you started:

- The "Alto User's Handbook" is indispensable and contains an overview of the 
  Alto Executive (the OS "shell"), Bravo (great-granddaddy of Microsoft Word) 
  and other utilities.  
  http://bitsavers.org/pdf/xerox/alto/Alto_Users_Handbook_Sep79.pdf
  
- "Alto Subsystems" documents many of the common Alto programs and tools
  ("subsystems" in Alto parlance) in detail.  
  http://bitsavers.org/pdf/xerox/alto/AltoSubsystems_Oct79.pdf

- "Alto Operating System Reference Manual" is useful if you are going to do
  any programming for the Alto.
  http://bitsavers.org/pdf/xerox/alto/AltoSWRef.part1.pdf
  http://bitsavers.org/pdf/xerox/alto/AltoSWRef.part2.pdf

- "BCPL Reference Manual" is definitely required if you are going to do any
  programming on the Alto (in BCPL, anyway...)
  http://bitsavers.org/pdf/xerox/alto/bcpl/AltoBCPLdoc.pdf

- "Bravo Course Outline" is a tutorial that will show you how to use the Bravo
  editor.
  http://bitsavers.org/pdf/xerox/alto/BravoCourse.pdf

- The "Alto Hardware Manual" is fun to read through if you're planning on
  writing an Alto emulator of your own.  If you're into that sort of thing.
  http://bitsavers.org/pdf/xerox/alto/AltoHWRef.part1.pdf
  http://bitsavers.org/pdf/xerox/alto/AltoHWRef.part2.pdf

- "A Field Guide to Alto-Land" is a casual perspective on Alto use (and
  the culture that grew around it) at Xerox PARC.
  http://xeroxalto.computerhistory.org/_cd8_/altodocs/.fieldguide.press!2.pdf
 

4.0 Configuration
=================

ContrAlto provides a number of configuration options via the 
"System->System Configuration..." menu.  Selecting this menu item will invoke
a small configuration dialog with three tabs, which are described in the
following sections.


4.1 Processor
-------------

This tab allows selection of the processor configuration.  Normally, this setting 
should not need to be changed from the default  (Alto II, 2K Control ROM, 
1K Control RAM).  If you need to run software that demands a specific 
configuration (which is very rarely the case) then change the configuration 
here.  The system will need to be reset for the change to take effect.

Additionally there are two execution options that govern emulator behavior:

The "Throttle Framerate" checkbox will force ContrAlto to run at an even 60
fields/second (matching the speed of the original Alto).  Use this if things 
are running too fast (for example, games that require reflexes.)  Uncheck this
if you want things to run as fast as possible (for example, compiling code or
running Smalltalk.)

Thee "Pause when ContrAlto window is not active" causes ContrAlto to pause
emulation if you switch to another window.


4.2 Ethernet
------------

The Ethernet tab provides configuration options for ContrAlto's host Ethernet
encapsulation.  ContrAlto can encapsulate the Alto's 3mbit ("experimental") 
Ethernet packets in either UDP datagrams or raw Ethernet packets on a network
interface on the "host" computer (the computer running ContrAlto).

Raw packet encapsulation requires libpcap libraries to be installed.  
On Windows, see https://npcap.com/.  On *nix, consult your distribution's 
package manager if not already installed; on Mac these should already be present.


4.2.1 Host Address
------------------

The Alto's network address can be specified via the "Alto Address" box at the
top of the tab.  This is an octal value between 1 and 376.  (The addresses 0
and 377 are reserved for broadcast and Breath Of Life packets, respectively.)

The default address is "42" and need only be changed if you will be
communicating with other Alto hosts on the network.  Duplicate network addresses
will cause odd problems in communication, so make sure all hosts have unique
addresses!


4.2.2 UDP Encapsulation
-----------------------

UDP Encapsulation is selected via the "UDP" radio button.  This causes Alto 
Ethernet packets to be encapsulated in broadcast UDP datagrams.  These 
broadcasts are sent to the IPV4 network associated with the network adapter
selected in the "Host Interface" network list box.


4.2.3 Raw Ethernet Encapsulation
--------------------------------

Raw Ethernet Encapsulation is selected via the "Raw Ethernet" radio button.
This causes Alto Ethernet packets to be encapsulated in ethernet packets on the
selected network interface.  This requires libpcap be present on the system
(see Section 4.2).


4.3 Display
-----------

The Display tab provides options governing the way ContrAlto displays the
simulated Alto display.






4.4 DAC
-------

The DAC tab provides options controlling the Audio DAC used by the Smalltalk
Music System.  "Enable Audio DAC" does what you'd expect -- it enables
or disables audio output (and audio capture).  If this option is enabled,
the "DAC Options" fields become available.

"Enable DAC output capture" enables the capture of audio generated by the DAC 
to be captured to one or more WAV files in the directory specified by the
"Output capture path" box.  This box specifies a directory, not a file --
when the Alto starts generating audio a new WAV file will be created in this
directory.  If the Alto is restarted or if ContrAlto exits, this file will
be closed.  WAV files created by ContrAlto are 16-bit, mono at 13Khz.


4.5 Printing
------------

The Printing tab provides options for the Orbit / Dover print system emulation.
The "Enable Printing" checkbox enables or disables print output.  If this option
is enabled, the "Printing Options" fields become available.

The "PDF output path" specifies the folder that output PDFs are written to.
When the Alto prints a new document, a new PDF file will be created in this
directory containing the printer's output.

The "Reverse Output Page Order" checkbox controls the order in which pages are
written to the PDF -- due to the way the original Dover printer worked, most
Alto software printed pages in reverse order (i.e. the last page printed
first) so that the pages didn't have to be reshuffled when picked up from the
tray.  By default, leaving this box checked is probably what you want, but
if your documents come out backwards, uncheck it.


4.6 Alternate ("keyboard") Boots
--------------------------------

The Alto allowed the specification of alternate boot addresses by holding down
a set of keys on the keyboard while hitting the "reset" switch on the back of
the keyboard.  Since this would be difficult to pull off by hand on the emulator
due to the UI involved, ContrAlto provides a configuration dialog to select the
alternate address to boot.  When the "Start with Alternate Boot" menu is
chosen, the system will be started (or restarted) with these keys held down on
your behalf.

The "Alternate Boot Options" dialog is invoked by the "System->Alternate Boot
Options" menu and provides configuration for alternate boots.

The boot type (disk or ethernet) can be selected via the "Alternate Boot Type"
radio buttons.  Ethernet booting will only work if another host on the network
is providing boot services.

The "Disk Boot Address" text box accepts a 16-bit octal value (from 0 to 177777)
specifying the address to be booted.

The "Ethernet Boot File" option provides a list box containing a number of
standard boot files, or a 16-bit octal value (from 0 to 177777) can be manually
supplied.


5.0 Debugger
============

ContrAlto contains a fairly capable debugger window that can be invoked via
the "System->Show Debugger" menu (or Ctrl+Alt+D) at any time.  When the debugger
is invoked, it takes over control of the system from the main display window.
The system can be micro-stepped or single-stepped and breakpoints can be set on
microcode addresses or Nova instruction addresses.

Usage of the debugger is mostly straightforward but it is intended for "expert"
users only and still has many rough edges.


5.1 The Controls
----------------
At the very bottom of the debugger window is a row of buttons.  These are (from
left to right):

Step:    Runs the Alto CPU for one clock cycle.  Normally this coincides with
         a single microinstruction, but not always (for example, memory accesses
         may require multiple cycles.)  The next  microinstruction to be 
         executed will be highlighted in the "Microcode Source" pane.

Auto:    Automatically single-steps the CPU at a relatively slow rate, while
         refreshing the debugger UI after every step.  Not particularly useful
         in most circumstances (but it looks neat.)
    
Run:     Starts the CPU running normally.  Execution will continue until a 
         breakpoint is hit one of the other control buttons are pressed.
         
Run T:   Runs the CPU until the next TASK switch occurs, the next instruction 
         executed will be the instruction after the TASK SF that caused the 
         switch.

Nova Step: Runs the CPU until the current Nova instruction is completed.  This
         will only work properly if the standard Nova microcode is running in 
         the Emulator task.
         
Stop:    Stops the CPU.

Reset:   Resets the Alto system.

5.2 Microcode Source Pane
-------------------------

The pane in the upper left of the debugger window shows the microcode listings
for ROM0, ROM1, and RAM0-RAM2.  The listings for ROM0 and ROM1 are derived from the
original source code listings.  The listing for the RAM banks is automatically 
disassembled from the contents of control RAM (and is generally more annoying
to read.)

ROM0 contains the listing for the main microcode ROMs -- this 1K of ROM contains
code for all of the microcode tasks (Emulator, Disk Sector, Ethernet, Memory
Refresh, Display Word, Cursor, Display Horizontal, Display Vertical, Parity, and
Disk Word).  The source code for each task is highlighted in a different color
to make task-specific code easy to differentiate.

ROM1 contains the listing for the Mesa 5.0 microcode ROMs.


5.3 Memory Pane
---------------

The pane near the top-middle (labeled "System Memory") shows a view into the main
memory of the Alto, providing address/data and a machine-generated disassembly 
of Alto (Nova) instructions.


5.4 Breakpoints
---------------

Breakpoints can be set on either microcode or Nova code by checking the box
in the "B" column next to the instruction.  Unchecking the box will remove the
breakpoint.

Nova code breakpoints will only work if the standard Nova microcode is running
in the Emulator task.


5.5 Everything Else
-------------------

The other panes in the debugger are:

Tasks:   Shows the current microcode task status.  The "T" column indicates the
         task name, "S" indicates the status ("W"akeup and "R"unning), and the
         "uPC" column indicates the micro-PC for the corresponding task.  There
         are 16 possible tasks, not all are used on most Altos.
         
CPU Registers:
         Shows the CPU L, M and T registers as well as ALU and memory registers.

General Registers:
         Shows the contents of the 32 R and 32 S registers (in octal).  
         (The extra 7 sets of R and S registers on 3K CRAM machines are not yet
         displayed.)
         
Reserved Memory:
         Shows the contents of most "well known" memory locations.  See the
         Alto HW Reference manual (link in Sectoin 3.1.2) for what these mean.


6.0 Scripting
=============

ContrAlto supports scripting of mouse and keyboard inputs as well as loading and
unloading of disk packs.  Scripts can be recorded live, using the emulator or
they can be handcrafted using a text editor of your choice.


6.1 Recording
-------------

Recording of a new script can be started by using the 
"File->Script->Record Script..." menu item.  You will be prompted for a
filename for the script after which recording will start.  Recording is only
active while the emulated Alto system is running.

During recording, mouse and keyboard inputs are captured, as are the following
actions:
    - Loading, unloading, or creating new disk packs
    - Resetting the emulated Alto
    - Quitting ContrAlto

Recording may be stopped at any time by using the "File->Script->Stop Recording"
menu.


6.2 Playback
------------

Playback of an existing script can be started by using the
"File->Script->Play Script..." menu item.  You will be prompted for a 
filename for the script after which playback will start.  If the emulated Alto
is not currently running when a script is played, it will be started.

During playback, mouse and keyboard input is disabled.  You can stop playback
at any time via the "File->Script->Stop Playback" menu item.


6.3 Script Format
-----------------
The script file format is very very basic; it’s plaintext, one entry per line.
Each entry is of the form:

[timestamp] [action]

Where [timestamp] is a relative time specified either in nanoseconds (no 
suffix) or milliseconds (‘ms’ suffix).  Additionally, a timestamp specified as
a “-“ (dash) indicates that the action should occur immediately (i.e. at a 
relative timestamp of 0). 

Any line beginning with "#" is a comment to end-of-line.

Each non-comment line specifies an action to take and the time 
(relative to the previous line) to execute it.  The first line’s execution time
is relative to the time at which the script is started.

There are a number of actions which can be specified, these are:

KeyDown [key]: Presses the specified key on the keyboard

KeyUp [key]: Releases the specified key on the keyboard

MouseDown [button]: Presses the specified mouse button (“Left”, “Right”, or “Middle”)

MouseUp [button]: Releases the specified mouse button

MouseMove [dx,dy]: Specifies a relative mouse movement.

MouseMoveAbsolute [x,y]: Specifies an absolute mouse movement.

Command [command string]: Executes the specified ContrAlto command (these are 
    identical to those typed at the debug console -- see readme-mono.txt), and all
    commands are supported except for those that display status (“show 
    trident disk”, for example).

KeyStroke [key1]...[keyN]: Presses and then releases the specified keys.  
    (e.g. "KeyStroke Ctrl A" will press Ctrl and A simultaneously, then release them.)

Type [string]: Sends keystrokes to type the given ASCII string.

TypeLine <string>: as above, but terminates with a CR.  <string> is optional.

Wait: Waits for the Alto to execute a STARTIO with bit 2 set (in the 
    Xerox bit ordering where MSB is bit 0).  This bit is not normally used; with 
    it a custom Alto program that sets this bit can wake up an executing script.

Valid values for KeyDown/KeyUp/KeyStroke are:
    A B C D E F G H I J K L M N O P Q R S T U V W X Y Z D0 D1 D2 D3 D4 D5 D6 D7
    D8 D9 Space Plus Minus Comma Period Semicolon Quote LBracket RBracket
    FSlash BSlash Arrow Lock LShift RShift LF BS DEL ESC TAB CTRL Return
    BlankTop BlankMiddle BlankBottom


7.0 Known Issues
================

- TriEx reports a status of "00000" randomly when doing read operations from
  Trident disks.  TFU and IFS work correctly.


8.0 Reporting Bugs
==================

If you believe you have found a new issue (or have a feature request) please
open an issue on github at https://github.com/jdersch/Contralto2.

When you send a report, please be as specific and detailed as possible:
- What issue are you seeing?
- What Alto software are you running?
- What operating system are you running ContrAlto on?
- What are the exact steps needed to reproduce the issue?

The more detailed the bug report, the more possible it is for me to track down
the cause.


9.0 Source Code
===============

The complete source code is available under the BSD 3-Clause License on GitHub at:

https://github.com/jdersch/Contralto2

Contributions are welcome!


10.0 Thanks and Acknowledgements
===============================

ContrAlto would not have been possible without the amazing preservation work of 
the Computer History Museum.

Ethernet encapsulation is provided courtesy of SharpPcap, a libpcap wrapper.
See: https://github.com/chmorgan/sharppcap.

Audio output and capture on Windows is provided using the SDL 2 libraries, see:
https://www.libsdl.org/.  This is exposed to C# via the the SDL2# wrapper, see:
https://github.com/flibitijibibo/SDL2-CS.

PDF generation is provided by the iText7 library, see: https://github.com/itext.


11.0 Change History
===================

V2.0.0
- Major refactoring, cleanup and rewrite using the AvaloniaUI cross-platform 
  UI framework.  Emulator should run essentially identically on all platforms.
- Updated to run under .NET 8.0 or later.
- Screen scaling, slow-phosphor emulation addeed.
- Fairly decent (10%) speed increase.

V1.2.3
------
- Added basic scripting support.
- Tweaked mouse handling to avoid Alto microcode bug and to smooth mouse
  movement.
- Fix for stale packets left in ethernet input queue when receiver is off.
- Minor code cleanup.

V1.2.2
------
- Initial support for the Trident controller and associated T-80 and T-300 
  drives.
- Added support for the Alto Keyset.  (Finally.)
- Fixed bug in XM bank register soft-reset behavior.  IFS now runs.
- Fixed issue with ethernet encapsulation that caused the emulator to receive
  its own packets.

V1.2.1
------
- Completed implementation of Orbit, Dover ROS and Dover print engine.
- Bugfixes to memory state machine around overlapped double-word reads/writes.
  Smalltalk-80 now runs, as does Spruce.

V1.2
----
- First release officially supporting Unix / OS X
- Audio DAC for use with Smalltalk Music system implemented
- Initial implementation of Orbit rasterization device; Dover ROS is implemented
  but not working properly.
- Added ability to load a configuration file at startup
- Switched to cross-platform SharpPcap library for Ethernet encapsulation.

V1.1
----
- A few minor performance tweaks, adding to a 10-15% speed increase.
- Switched back to targeting .NET 4.5.3 rather than 4.6; this works better under Mono
  and avoids odd issues on Windows machines running pre-4.6 frameworks.
- Microcode disassembly improved slightly, annotated microcode source updated.
- Nova disassembler now handles BRI, DIR, EIR, DIRS instructions rather than treating
  them all as TRAPs.
- Fixed serious bugs in memory state machine, BravoX now runs.
- Fixed minor bug in Constant ROM selection.
- Raw Ethernet packets sent as broadcasts (matching IFS encapsulation behavior)

V1.0
----
Initial release.