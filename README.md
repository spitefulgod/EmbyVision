# EmbyVision
Verbose speech interface for Emby Media centre, specifically created for Blind / Partially sighted users

First off, I'd also like to thank the Emby media team for a fantastic product and amazing support

This command line software allows connection to emby connect via an emby account or LAN Machine, it then lets you issue voice commands to control an already running emby client on the machine.

Run the project, the system will search for LAN Emby servers and connect to any that have servers with no passwords

Type Help at the command prompt for a list of basic admin commands

EnableEmbyBasic [Username] [Password] will allow you to specify a username and password to connect to LAN machines
e.g. EnableEmbyBasic Spitefulgod MyPassword

EnableEmbyConnect [Username] [Password] will allow you to authenticate with Emby Connect giving you access to servers on that account
e.g. EnableEmbyConnect Spitefulgod MyPassword

The system will remember the last connected server and attempt to connect to that server first, if emby connect is unavailable the system will fallback to LAN servers


For the software to work a client must be available to a server, by default the software will connect to the client with the most supported commands and remote control access.  On my system the client starts automatically at startup along with the software.

Once connected to a valid server with a valid client you can then issue voice commands.  I must state at this point that this system is not designed for use with a computer microphone or continuous speech input, the microsft speech engine is just too flaky and will result in many false positives.  This software should be used with a microphone that can be activated when needed for speech input, e.g. a amazon fire stick remote or my personal preference the Amulet Voice remote control.


Speech commands can be emulated by typing them into the command prompt prefixed with a colon (:)
e.g. :Watch Movie Contact

A list of viable speech commands is available on the [wiki](https://github.com/spitefulgod/EmbyVision/wiki)

There's plenty left to do on this and will be ongoing for myself as both my parents are blind and currently rely on this for thier media needs, there is currently no music support or splitting of media folders, feel free to chip in.
