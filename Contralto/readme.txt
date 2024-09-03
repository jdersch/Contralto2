Readme.txt for ContrAlto v2.0.0 Beta:

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
   - Standard Keyboard/Mouse/Video hardware
   - Alto Keyset (5-key chording keyboard)
   - Audio DAC (used with the Smalltalk Music System)
   - The Orbit raster hardware, Dover Raster Output Scanner and Dover print
     engine, which provides 384dpi print output (currently to PDF only)
   - The Trident Disk Controller (TriCon) and up to eight T-80 or T-300 
     drives

1.2 What's Not
--------------

At this time, ContrAlto does not support more exotic hardware such as the Diablo
printer, serial and IMP interfaces, or audio using the utility port.


2.0 Requirements
================

ContrAlto will run on any system capable of running version 8.0 or later of the 
Microsoft .NET Runtime.  If it is not installed on your computer, instructions 
for getting it can be obtained at https://dotnet.microsoft.com/en-us/download/dotnet/8.0.
You need only install the runtime, the full SDK is not required unless you are
planning to build ContrAlto from sources.

A three-button mouse is essential for using some Alto software.  On most mice,
the mousewheel can be clicked to provide the third (middle) button.  Laptops
with trackpads may have configuration options to simulate three buttons but
will likely be clumsy to use.


3.0 Getting Started
===================

Installation of ContrAlto is simple:  Unzip the release archive to a directory
of your choosing.

To run the emulator:

Windows: double-click Contralto.exe in Explorer

Mac/*nix: Run "dotnet Contralto.dll" in a shell -- please see section 2.0 for 
          obtaining the .NET runtime for your platform.)


3.1 Starting the Alto
=====================

On a real Alto, the system is booted by loading a 14" disk pack into the front
of a Diablo 31 drive, waiting for it to spin up for 20 seconds and then
pressing the "Reset" button on the back of the keyboard.

Booting an emulated Alto under ContrAlto is slightly less time-consuming.
To load a disk pack into the virtual Diablo drive, click on the "File"
menu and go to "Diablo Drive 0 -> Load...".  You will be presented with a file
picker allowing selection of the disk image (effectively a "virtual disk pack")
to be loaded.  Disk images are included with ContrAlto in the "Disks" directory
and may also be found in various places on the Internet -- see Section 3.1.4 for
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

The Alto mouse has three-buttons.  Alto mouse buttons are mapped to your computer's
mouse as you would expect.  If you have a real three-button mouse then this is 
completely straightforward.  If you have a two button mouse with a "mousewheel" then
a mousewheel click maps to a click of the Alto's middle mouse button.

If you have a trackpad or other pointing device, using the middle mouse button 
may be more complicated.  See what configuration options your operating system 
and/or drivers provide you for mapping mouse buttons or gestures, or consider
using an external three-button mouse.


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
in the early days of the Alto, inherited from Engelbart's pioneering work on NLS.
(It never caught on.)  The 5 keys on the keyset are mapped to F5-F9 on your 
keyboard, with F5 corresponding to the leftmost key and F9 corresponding to the 
rightmost.

Not a ton of software made use of the keyset -- Larry Tesler's "Gypsy" system
and the "ugh" editor require its use.  Mazewar can optionally use it for
navigating through the maze.


3.1.4 Diablo Disk Packs
-----------------------

A real Alto uses large 14" disk packs for disk storage, each containing
approximately 2.5 megabytes (for Diablo 31) or 5 megabytes (for Diablo 44) of
data.  ContrAlto uses files referred to as "disk images" (or just "images")
that contain a bit-for-bit copy of these original packs.  These are a lot
easier to use with a modern PC.

Disk images can be loaded, unloaded and created via the "File->Diablo Drive 0" 
and "File->Diablo Drive 1" menus.  A file picker will be presented showing 
possible disk images in the current directory.

If you modify the contents of a loaded disk (for example creating new files or
deleting existing ones) the changes will be written back out to the disk image
when a new image is loaded or when ContrAlto exits.  For this reason it may be
a good idea to make backups of packs from time to time (just like on the real
machine.)

ContrAlto comes with a small set of curated Diablo disk images in the "Disks"
directory:

animation.dsk:  The Smalltalk-72 Animation System.  

bcpl.dsk:  Contains the BCPL compiler and associated utilities and tools used 
           for building BCPL programs.

bravox54.dsk:  BravoX, version 5.4.  BravoX was an update to the original
               Bravo WYSIWYG text editor, written in Mesa.

diag.dsk:  A collection of diagnostic utilities.

games.dsk:  Every game for the Alto that I've been able to find.  (Programs that 
            end in ".boot" can be started using "bootfrom" -- e.g. 
            "bootfrom astroroids.boot")

music.dsk:  The Smalltalk-72 Music System, which makes use of Steve Saunders's
            8-voice FM synthesizer microcode.  ContrAlto must be configured as
            an Alto I system for this to run properly.

nonprog.dsk:  The "Non-Programmer's Disk" -- contains Bravo and Laurel (e-mail client)

spruce-server.dsk:  Contains the Spruce print server, configured for a Dover printer.
                    Must be used with spruce-server.t300 loaded in Trident drive 0.
                    (The Trident pack contains fonts and spooling space.)

xmsmall.dsk:  Smalltalk-76 for XM systems.  ContrAlto must be configured as an
              Alto II XM to run.  Start with "resume xmsmall.boot" from the executive.


Additionally and an assortment of Alto programs can be found on Bitsavers.org, at 
http://www.bitsavers.org/bits/Xerox/Alto/disk_images/.

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

ContrAlto comes with a single Trident disk image, spruce-server.t300.  This is intended
to be used in tandem with the "spruce-server.dsk" Diablo pack image to run the Spruce
print server.


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
excellent repository of original Alto documentation, as is CHM's PARC archive
at https://xeroxalto.computerhistory.org/.  Here are a few documents to 
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

- The "Smalltalk-72 Instruction Manual" is a useful guide for playing with
  the ST-72 systems on the animation and music packs.
  https://bitsavers.org/pdf/xerox/smalltalk/Smalltalk-72_Instruction_Manual_Mar76.pdf
  (See also: https://bitsavers.org/pdf/xerox/smalltalk/music-help-1976.pdf and
  https://bitsavers.org/pdf/xerox/smalltalk/simpled.doc.pdf)


4.0 Configuration
=================

ContrAlto provides a number of configuration options via the 
"System->System Configuration..." menu.  Selecting this menu item will invoke
a small configuration dialog with multiple tabs, which are described in the
following sections.


4.1 Processor
-------------

This tab allows selection of the processor configuration.  Normally, this setting 
should not need to be changed from the default  (Alto II, 2K Control ROM, 
1K Control RAM).  If you need to run software that demands a specific 
configuration (which is very rarely the case) then change the configuration 
here.  ContrAlto will need to be restarted for the change to take effect.

Additionally there are two execution options that govern emulator behavior:

The "Throttle Framerate" checkbox will force ContrAlto to run at an even 60
fields/second (matching the speed of the original Alto).  Use this if things 
are running too fast (for example, games that require reflexes.)  Uncheck this
if you want things to run as fast as possible (for example, compiling code or
running Smalltalk.)

Thee "Pause when ContrAlto window is not active" causes ContrAlto to pause
emulation if you switch to another window.  This may be useful to save battery
on laptops.


4.2 Ethernet
------------

The Ethernet tab provides configuration options for ContrAlto's host Ethernet
encapsulation.  ContrAlto can encapsulate the Alto's 3mbit ("experimental") 
Ethernet packets in either UDP datagrams or raw Ethernet packets over a network
interface on the "host" computer (the computer running ContrAlto).

Raw packet encapsulation requires libpcap libraries to be installed.
On Windows, see https://npcap.com/.  On *nix, consult your distribution's
package manager if not already installed; on Mac systems these will already
be present.


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

"Simulate Slow Phosphor Persistence" enables or disables a cheap attempt at simulating
the decay of the original Alto's CRT, which used P40 phosphor.

"Screen Scaling" allows scaling the emulated Alto's display by an integer scale factor.
This is useful on high-DPI displays.


4.4 DAC
-------

The DAC tab provides options that control the Audio DAC used by the Smalltalk
Music System.  "Enable Audio DAC" does what you'd expect -- it enables
or disables audio output.


4.5 Printing
------------

The Printing tab provides options for the Orbit / Dover print system emulation.
The "Enable Printing" checkbox enables or disables print output.  If this option
is enabled, the "Printing Options" fields become available.

The "PDF output path" box specifies the folder that output PDFs are written to.
When the Alto prints a new document, a new PDF file will be created in this
directory containing the printer's output.

The "Raster X Offset" and "Raster Y Offset" boxes allow you to adjust the
position of the print output on the page by specifying X and Y offsets.  This
ought to be fine as is, but if you want to tweak it, by all  means...

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
supplied.  In order to make use of Etherboot, ContrAlto must have its networking
configured (see section 4.2) and a properly configured IFS service must be running 
(either on another Alto, or using a system such as https://github.com/jdersch/IFS)


5.0 Debugger
============

The debugger present in previous versions of ContrAlto has not yet been reimplemented
for V2.0.  But watch this space!


6.0 Scripting
=============

ContrAlto supports scripting of mouse and keyboard inputs as well as loading and
unloading of disk packs.  Scripts can be recorded live using the emulator or
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

Command [command string]: Executes the specified ContrAlto command (see section 6.4).

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


6.4 Script Commands
-------------------

The below commands may be used with the "Command" action in a script:

Quit - Exits ContrAlto.  Any modifications to loaded disk images are saved.

Start - Starts the emulated Alto system.

Stop - Stops the emulated Alto.

Reset - Resets the emulated Alto.

Start With Keyboard Disk Boot - Starts the emulated Alto with the keyboard disk boot address specified
                                either in the configuration file or by the Set Keyboard Disk Boot Address
                                command.

Start With Keyboard Net Boot - Starts the emulated Alto with the keyboard ethernet boot number specified
                               either in the configuration file or by the Set Keyboard Net Boot File
                               command.

Load Disk <drive> <path> - Loads the specified Diablo drive (0 or 1) with the requested disk image.

Unload Disk <drive> - Unloads the specified Diablo drive (0 or 1).  Changes to disk contents are saved.

New Disk <drive> - Creates a new (empty) disk image and loads the specified Diablo drive with it.

Load Trident <drive> <path> - Loads the specified Trident drive (0 through 7) with the requested 
                              disk image.

Unload Trident <drive> - Unloads the specified Trident drive (0 through 7).  Changes to disk 
                         contents are saved.

New Trident <drive> - Creates a new (empty) disk image and loads the specified Trident drive with it.
                         Specifying a file extension of ".dsk80" will create a new T-80 disk; an extension
                         of ".dsk300" will create a new T-300 disk. 

Show Trident <drive> - Displays the currently loaded image for the specified drive (0 through 7).

Show System Type - Displays the Alto system type as configured by the configuration file.

Set Ethernet Address - Sets the Alto's host Ethernet address.  Values between 1 and 376 (octal) are
                       allowed.

Set Keyboard Net Boot File - Sets the boot file used for net booting.  Values between 0 and 177777
                             are allowed.

Set Keyboard Disk Boot Address - Sets the boot address used for disk booting.  Values between 0 and 
                                 177777 are allowed.

Start Playback <script path> - Begins playback of the specified script file.

Stop Playback - Ends any active script being replayed.


7.0 Configuration File Format
=============================

ContrAlto can be configured through the use of configuration files.  These
are simple text files with sets of parameters and their values in the form:

  ParameterName = Value

At startup, ContrAlto looks for configuration data in a file named ContrAlto.cfg
in the currnent working directory at startup.  Alternate configuration files can 
be specified as a  command-line argument at startup via the "-config" parameter.
(e.g. ""ContrAlto.exe -config <configuration file>")

An example configuration file looks something like:

    # contralto.cfg:
    #
    # This file contains configuration parameters for ContrAlto.
    # All integers are specified in octal.
    #

    # System configuration
    SystemType = TwoKRom
    HostAddress = 42

    # Host networking configuration
    HostPacketInterfaceType = EthernetEncapsulation
    HostPacketInterfaceName = eth0
    
    # Emulation Options
    ThrottleSpeed = True
    BootAddress = 0
    BootFile = 0
    AlternateBootType = Ethernet

    # Display / Interface options
    KioskMode = False
    AllowKioskExit = True
    PauseWhenNotActive = False
    FullScreenStretch = False
    SlowPhosphorSimulation = True
    DisplayScale = 1

    # Printing options
    EnablePrinting = true
    PrintOutputPath = .
    ReversePageOrder = true
    PageRasterOffsetX = 0
    PageRasterOffsetY = 0

    # Disk options
    #

    # Diablo images:  These specify a single image file for drive 0 and drive 1
    #
    Drive0Image = Disks\diag.dsk
    # Drive1Image =

    # Trident images:  This specifies up to eight images for Trident drives 0 through 8
    #                  in a comma-delimited list.  Empty entries are allowed, as below:
    # TridentImages =  image0.dsk80, , image2.dsk300, image3.disk80


The following parameters are configurable:

SystemType:  Selects the type of Alto system to emulate.  One of:
    - AltoI     : an Alto I, with 64KW memory, 1K ROM, and 1K CRAM
    - OneKRom   : an Alto II XM system with 1K ROM, 1K CRAM
    - TwoKRom   : an Alto II XM system with 2K ROM, 1K CRAM
    - ThreeKRam : an Alto II XM system with 1K ROM, 3K CRAM
    The default is TwoKRom.

HostAddress:  Specifies the Alto's Ethernet address (in octal).  Any value
              between 1 and 376 is allowed.

HostPacketInterfaceType:  Specifies the type of interface to be used on the 
              host for Ethernet emulation.  One of:
    - UDPEncapsulation: Transmits Alto Ethernet packets over UDP broadcasts
    - EthernetEncapsulation: Transmits Alto Ethernet packets over raw Ethernet packets.
            (See Section 4.1 for configuration details)
    - None: No packet encapsulation.

HostPacketInterfaceName:  Specifies the name of the host network interface
              to use for Ethernet emulation.  (e.g. "eth0" or "Ethernet")  If no network
              interface is to be used, this parameter can be omitted.

BootAddress: The address to use with a Keyboard Disk Boot (See section 5.0)

BootFile:    The file number to use with a Keyboard Net Boot (again, Section 5.0)

AlternateBootType:  The type of boot to default to (Section 5.0)

ThrottleSpeed:      Enables or disables speed throttling.  When enabled, emulation speed
                    will be limited to 60 fields/sec (i.e. real Alto speed).

KioskMode:          Enables a special mode intended for unattended displays of the emulator
                    where a fullscreen display without the ability for users to interact
                    with emulator configuration, etc.  It effectively makes the emulator's
                    execution tamper-proof

AllowKioskExit:     Allows exiting the emulator via the Ctrl+Alt+X keyboard shortcut
                    while in Kiosk Mode.

PauseWhenNotActive: Causes the emulator to pause when the emulator's window is not active.

SlowPhosphorSimulation:  Provides a crude simulation of the slow phosphor of the Alto's CRT.

DisplayScale:       An integer value specifying a scale factor for the display.  Useful
                    for high-DPI displays.

EnablePrinting:     Enables or disables printing via the emulated Orbit / Dover interface.

PrintOutputPath:    Specifies the folder that output PDFs are written to. When the Alto 
                    prints a new document, a new PDF file will be created in this
                    directory containing the printer's output.

ReversePageOrder:   Controls the order in which pages are written to the PDF -- due to 
                    the way the original Dover printer worked, most Alto software printed 
                    documents in reverse order (i.e. the last page printed first) so 
                    that the pages didn't have to be reshuffled when picked up from the
                    tray.  By default, setting this to true is probably what you want, 
                    but if your documents come out backwards, set it to false.

PageRasterOffsetX/Y:  Allows tweaking of the centering of the Orbit/Dover raster on the page.

Drive0Image and Drive1Image:  Specifies a disk image to be loaded into the 
                              specified drive.  These parameters are optional.

TridentImages:      Allows specifying up to eight Trident disk images in a comma-delimited list.
                    Empty entries are allowed to indicate drives that do not have a pack loaded.

When the emulator is exited, settings changed via the UI are written back out to the 
configuration file ContrAlto was started with.

8.0 Known Issues
================

- TriEx reports a status of "00000" randomly when doing read operations from
  Trident disks.  TFU and IFS work correctly.
- The debugger available in the 1.x releases has not yet been re-implemented.
- Audio capture (to .wav or similar) has similarly not yet been re-implemented.
- Screen scaling and fullscreen options may have issues depending on platform, window manager,
  and your display's DPI settings.  Please feel free to report this as it's impossible for me
  to test this on all possible configurations of hardware and operating system.


9.0 Reporting Bugs
==================

If you believe you have found a new issue (or have a feature request) please
open an issue on github at https://github.com/jdersch/Contralto2.

When you send a report, please be as specific and detailed as possible:
- What issue are you seeing?
- What Alto software are you running?
- What operating system and windowing environment are you running ContrAlto on?
- What are the exact steps needed to reproduce the issue?

The more detailed the bug report, the more possible it is for me to track down
the cause.


10.0 Source Code
================

The complete source code is available under the BSD 3-Clause License on GitHub at:

https://github.com/jdersch/Contralto2

Contributions are welcome!


11.0 Thanks and Acknowledgements
===============================

ContrAlto would not have been possible without the amazing preservation work of
the Computer History Museum and Al Kossow.

Version 1.0 was written by the author while working for the Living Computer Museum
in Seattle, WA -- may it rest in peace.  Sorry, Paul -- we tried.

Ethernet encapsulation is provided courtesy of SharpPcap, a libpcap wrapper.
See: https://github.com/chmorgan/sharppcap.

Audio output is provided using the SDL 2 libraries, see: https://www.libsdl.org/.
This is exposed to C# via the the SDL2# wrapper, see: 
https://github.com/flibitijibibo/SDL2-CS.

PDF generation is provided by the iText7 library, see: https://github.com/itext.

And of course: Thanks to all of the PARC engineers who designed and built the Alto,
its software, and the future.


12.0 Change History
===================

V2.0.0
- Major refactoring, cleanup and rewrite using the AvaloniaUI cross-platform 
  UI framework.  Emulator should run essentially identically on all platforms.
- Updated to run under .NET 8.0 or later.  Mono is no longer supported.  Sorry.
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