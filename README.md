# AM2RModPacker
Mod packaging toolchain for [AM2RLauncher](https://github.com/AM2R-Community-Developers/AM2RLauncher) mods.

This is for modcreators who wish to make their mods usable with the [AM2RLauncher](https://github.com/AM2R-Community-Developers/AM2RLauncher).
It currently supports creating Windows, Linux and Android mods.

For actual mod creation, it is recommended to use Game Maker: Studio and the [AM2R Community Updates repository](https://github.com/AM2R-Community-Developers/AM2R-Community-Updates), but they can be created with [UndertaleModTool](https://github.com/krzys-h/UndertaleModTool) as well. 

## Usage instructions
![grafik](https://user-images.githubusercontent.com/38186597/130315943-4ae7b97d-0ded-4d0d-830f-c779b0ad934a.png)

* Mod name: Specify the name of the mod
* Author: Specify the authors of the mod
* Version: Specify the version of the mod
* Mod notes (optional): Specify notes the users will see about the mod. Consider this like a readme.
* Uses custom save directory: Specify wether or not your mod uses a custom save directory and if yes, which one. Only save locations in `%localappdata` are supported
* Uses custom music: Specify wether or not your mod has custom music in it.
* Uses the YoYo Compiler: Specify wether or not your mod was built with the YoYo Compiler or not. A quick way to find out, is try opening the `data.win` file in the above mentioned UndertaleModTool. If the tool mentions that it was built with YYC, or the file does not exist, it's YYC.
* Supports Android: Specify wether or not your mod supports Android, and if yes load in the modded Android APK
* Supports Linux: Specify wether or not your mod supports Linux, and if yes load in the modded Linux zip. For GameMaker: Studio users, you can just use the one that gets created. For non GM:S users, please make sure that the executable is either named `AM2R` or `runner` and that neither the `assets` folder nor the executable is in a subfolder.
* Load 1.1: Select your valid 1.1 Zip. If you get an error, make sure that the game isn't in any subfolder.
* Load modded game: Select a zip of your modded game for Windows. If you get a warning, make sure that your mod is not placed in any subfolder.

After filling everything out appropriately, just click on the `Create Mod package(s)` button, which will ask you where to save your AM2RLauncher-compatible mod and then create them.
