# AM2R Modpacker
This tool packs your mods into a format usable by the [AM2R Autopatcher](https://github.com/Lojemiru/AM2R-Autopatcher "AM2R Autopatcher"). There exists both a GUI (Windows only) and a CLI (cross-platform) version.

## GUI
The only requirement is that you have at least a 32-bit version of Windows installed.
Android patching is currently disabled until a bug is fixed that prevents them from being played on real hardware.

## CLI
You need to have at least .NET Core runtime 3.0 installed. The latest version of it, can be found [here](http:https://dotnet.microsoft.com/download/dotnet/// "here"). If you are on Linux or on MacOS you also need to have the xdelta3 package installed. Refer to your local package manager for further instructions on how to do it.
General usage is the following:

AM2RModPackerConsole --name NAME --author AUTHOR --original ORIGINALPATH --mod MODPATH [--custommusic] [--savedata] [--yoyo]

Description:
-n, --name<br>
	The name of the mod

-a, --author<br>
	The name of the author

-o, --original<br>
	The path to the AM2R_11.zip file

-m, --mod<br>
	The path to your custom AM2R.zip

-c, --custommusic<br>
	Specify if your mod uses custom music

-s, --savedata<br>
	Specify if your mod uses a custom savedata folder

-y, --yoyo<br>
	Specify if your mod uses the YoYo compiler instead of the normal one

-h, --help<br>
	Displays this help and exit

-v, --verbose<br>
	Uses verbose mode