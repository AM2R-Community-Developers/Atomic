using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Diagnostics;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace AM2RModPackerConsole
{
    class Program
    {
        enum parameterValues
        {
            invalid,
            name,
            author,
            original,
            mod
        }

        static string[] viableParameters = { "-n", "--name", "-a", "--author", "-o", "--original", "-m", "--mod", "-c", "--custommusic", "-s", "--savedata", "-y", "-yoyo", "-v", "--verbose" };
        static string[] helpParameter = { "-h", "--help", "?" };
        static string[] verboseParameter = { "-v", "--verbose" };
        static ModProfile profile = new ModProfile(1, "", "", false, "default", false, false);
        static string originalPath, modPath, apkPath, localPath = Directory.GetCurrentDirectory();
        static char sep = Path.DirectorySeparatorChar;
        static readonly string ORIGINAL_MD5 = "f2b84fe5ba64cb64e284be1066ca08ee";
        static readonly string[] DATAFILES_BLACKLIST = { "data.win", "AM2R.exe", "D3DX9_43.dll" };
        static Boolean isCurrentOSWindows = System.Environment.OSVersion.Platform == PlatformID.Win32NT;
        static Boolean isVerboseOn = false;

        static void Main(string[] args)
        {         
            //if no parameters were given, display help
            if(args.Length==0)
            {
                DisplayHelp();
                return;
            }

            //check if help parameter was put in       
            if(helpParameter.Contains(args[0]))
            {
                DisplayHelp();
                return;
            }

            //check if arguments have any verbose parameters in them
            if (args.Intersect(verboseParameter).Any())
                isVerboseOn = true;

            //check the other parameters
                parameterValues lastParameter = parameterValues.invalid;
            foreach(var argument in args)
            {
                if(isVerboseOn)
                    Console.WriteLine($"DEBUG: Current parameter: {argument}");

                //is this a valid parameter?
                if(lastParameter == parameterValues.invalid && !viableParameters.Contains(argument))
                {
                    Console.WriteLine("Invalid Parameter! Please refer to the \"--help\" command for usage.");
                    return;
                }

                //the last parameter was valid, so we continue parsing it

                if(lastParameter != parameterValues.invalid)
                {
                    //the "s from the arguments are stripped already by c# stuff, means we don't have to do it manually
                    switch (lastParameter)
                    {
                        case parameterValues.name:
                            profile.name = argument;
                            if (isVerboseOn)
                                Console.WriteLine($"DEBUG: {argument} has been set as name");
                            break;

                        case parameterValues.author:
                            profile.author = argument;
                            if (isVerboseOn)
                                Console.WriteLine($"DEBUG: {argument} has been set as author");
                            break;

                        case parameterValues.original:
                            originalPath = argument;
                            if (isVerboseOn)
                                Console.WriteLine($"DEBUG: {argument} has been set as the original Path");
                            break;

                        case parameterValues.mod:
                            modPath = argument;
                            if (isVerboseOn)
                                Console.WriteLine($"DEBUG: {argument} has been set as the mod Path");
                            break;                            
                    }
                    lastParameter = parameterValues.invalid;
                    continue;
                }

                //parse Parameter, if the parameter is not invalid, that means that the next parameter will be one outside of the viable parameter range
                lastParameter = ReturnParameterValue(argument);

                if (isVerboseOn && lastParameter != parameterValues.invalid)
                    Console.WriteLine($"DEBUG: Recgonized current argument as {lastParameter}");

                //this is a stupid hack, and would probably break if this would be further expanded, but at the moment it works.
                //if someone knows how to make this better, please tell me
                if (lastParameter == parameterValues.invalid)
                {
                    //c for custom music
                    if (argument.Contains('c'))
                    {
                        profile.usesCustomMusic = true;
                        if (isVerboseOn)
                            Console.WriteLine($"DEBUG: {argument} has been seen as custom music");
                    }
                    //d for saveData
                    else if (argument.Contains('d'))
                    {
                        profile.saveLocation = "custom";
                        if (isVerboseOn)
                            Console.WriteLine($"DEBUG: {argument} has been seen as custom savedata");
                    }
                    //y for yoyo
                    else if (argument.Contains('y'))
                    {
                        profile.usesYYC = true;
                        if (isVerboseOn)
                            Console.WriteLine($"DEBUG: {argument} has been seen as yoyo compiler");
                    }
                }
            }
            if (profile.name == "" || profile.author == "" || modPath == null || originalPath == null)
            {
                Console.WriteLine("Not all necessary parameters were filled out! Please refer to the \"--help\" command for further information.");
                if (isVerboseOn)
                    Console.WriteLine($"DEBUG: name is currently {profile.name}, author is currently {profile.author}, original path is currently {originalPath}, mod path is currently {modPath}");
                return;
            }


            #region patching

            //the actual patching begins here
            //atm it's just mostly copypasted from the winforms project, would be better, if we could put this into a file that both projects could access
            Console.WriteLine("Patching begins...");
            string output = localPath + sep + profile.name + ".zip";

            // Cleanup in case of previous errors
            if (Directory.Exists(Path.GetTempPath() + $"{sep}AM2RModPacker"))
            {
                Directory.Delete(Path.GetTempPath() + $"{sep}AM2RModPacker", true);
            }

            // Create temp work folders
            string tempPath = "",
                   tempOriginalPath = "",
                   tempModPath = "",
                   tempProfilePath = "";

            // We might not have permission to access to the temp directory, so we need to catch the exception.
            try
            {
                tempPath = Directory.CreateDirectory(Path.GetTempPath() + $"{sep}AM2RModPacker").FullName;
                tempOriginalPath = Directory.CreateDirectory(tempPath + $"{sep}original").FullName;
                tempModPath = Directory.CreateDirectory(tempPath + $"{sep}mod").FullName;
                tempProfilePath = Directory.CreateDirectory(tempPath + $"{sep}profile").FullName;

                if (isVerboseOn)
                {
                    Console.WriteLine($"DEBUG: {tempPath} has been set as tempPath");
                    Console.WriteLine($"DEBUG: {tempOriginalPath} has been set as tempOriginalPath");
                    Console.WriteLine($"DEBUG: {tempModPath} has been set as tempModPath");
                    Console.WriteLine($"DEBUG: {tempProfilePath} has been set as tempProfilePath");
                }
            }
            catch (System.Security.SecurityException)
            {
                Console.WriteLine("Could not create temp directory! Please run the application with administrator rights.");
                AbortPatch();
                return;
            }
            // Extract 1.1 and modded AM2R to their own directories in temp work
            ZipFile.ExtractToDirectory(originalPath, tempOriginalPath);
            ZipFile.ExtractToDirectory(modPath, tempModPath);

            if (isVerboseOn)
                Console.WriteLine("DEBUG: Successfully zipped files.");

            // Verify 1.1 with an MD5. If it does not match, exit cleanly and provide a warning window.
            try
            {
                string newMD5 = CalculateMD5(tempOriginalPath + $"{sep}data.win");

                if (isVerboseOn)
                    Console.WriteLine($"DEBUG: Calculated MD5: {newMD5}");

                if (newMD5 != ORIGINAL_MD5)
                {
                    // Show error
                    Console.WriteLine("1.1 data.win does not meet MD5 checksum! Mod packaging aborted.\n1.1 MD5: " + ORIGINAL_MD5 + "\nYour MD5: " + newMD5);
                    AbortPatch();

                    return;
                }
            }
            catch (FileNotFoundException)
            {
                // Show error message
                Console.WriteLine("data.win not found! Are you sure you selected AM2R 1.1? Mod packaging aborted.");
                AbortPatch();

                return;
            }

            // Create AM2R.exe and data.win patches
            int patchResult;
            if (profile.usesYYC)
            {
                patchResult = CreatePatch(tempOriginalPath + $"{sep}data.win", tempModPath + $"{sep}AM2R.exe", tempProfilePath + $"{sep}AM2R.xdelta");
            }
            else
            {
                patchResult = CreatePatch(tempOriginalPath + $"{sep}data.win", tempModPath + $"{sep}data.win", tempProfilePath + $"{sep}data.xdelta");

                patchResult = CreatePatch(tempOriginalPath + $"{sep}AM2R.exe", tempModPath + $"{sep}AM2R.exe", tempProfilePath + $"{sep}AM2R.xdelta");
            }

            if (patchResult == -1)
            {
                AbortPatch();
                Console.WriteLine("Mod packaging aborted!");
                return;
            }

            if (isVerboseOn)
                Console.WriteLine("DEBUG: Successfully patched.");

            // Create game.droid patch and wrapper if Android is supported
            //this is still todo.
            if (profile.android)
            {
                string tempAndroid = Directory.CreateDirectory(tempPath + $"{sep}android").FullName;

                // Extract APK 
                // - java -jar apktool.jar d "%~dp0AM2RWrapper_old.apk"

                // Process startInfo
                ProcessStartInfo procStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = tempAndroid,
                    Arguments = "/C java -jar \"" + localPath + $"{sep}utilities{sep}android{sep}apktool.jar\" d -f -o \"" + tempAndroid + "\" \"" + apkPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Run process
                using (Process proc = new Process { StartInfo = procStartInfo })
                {
                    proc.Start();

                    proc.WaitForExit();
                }

                // Create game.droid patch
                CreatePatch(tempOriginalPath + $"{sep}data.win", tempAndroid + $"{sep}assets{sep}game.droid", tempProfilePath + $"{sep}droid.xdelta");

                // Delete excess files in APK

                // Create whitelist
                string[] whitelist = { "splash.png", "portrait_splash.png" };

                // Get directory
                DirectoryInfo androidAssets = new DirectoryInfo(tempAndroid + $"{sep}assets");

                // Copy *.ini to profile, rename to AM2R.profile


                // Delete files
                foreach (FileInfo file in androidAssets.GetFiles())
                {
                    if (file.Name.EndsWith(".ini") && file.Name != "modifiers.ini")
                    {
                        if (File.Exists(tempProfilePath + $"{sep}AM2R.ini"))
                        {
                            // This shouldn't be a problem... normally...
                            File.Delete(tempProfilePath + $"{sep}AM2R.ini");
                        }
                        File.Copy(file.FullName, tempProfilePath + $"{sep}AM2R.ini");
                    }

                    if (!whitelist.Contains(file.Name))
                    {
                        File.Delete(file.FullName);
                    }
                }

                foreach (DirectoryInfo dir in androidAssets.GetDirectories())
                {
                    Directory.Delete(dir.FullName, true);
                }

                // Create wrapper

                // Process startInfo
                // - java -jar apktool.jar b "%~dp0AM2RWrapper_old" -o "%~dp0AM2RWrapper.apk"
                ProcessStartInfo procStartInfo2 = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = tempAndroid,
                    Arguments = "/C java -jar \"" + localPath + $"{sep}utilities{sep}android{sep}apktool.jar\" b -f \"" + tempAndroid + "\" -o \"" + tempProfilePath + $"{sep}AM2RWrapper.apk\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Run process
                using (Process proc = new Process { StartInfo = procStartInfo2 })
                {
                    proc.Start();

                    proc.WaitForExit();
                }

            }

            // Copy datafiles (exclude .ogg if custom music is not selected)

            DirectoryInfo dinfo = new DirectoryInfo(tempModPath);

            Directory.CreateDirectory(tempProfilePath + $"{sep}files_to_copy");

            if (profile.usesCustomMusic)
            {
                // Copy files, excluding the blacklist
                CopyFilesRecursive(dinfo, DATAFILES_BLACKLIST, tempProfilePath + $"{sep}files_to_copy");
            }
            else
            {
                // Get list of 1.1's music files
                string[] musFiles = Directory.GetFiles(tempOriginalPath, "*.ogg").Select(file => Path.GetFileName(file)).ToArray();

                // Combine musFiles with the known datafiles for a blacklist
                string[] blacklist = musFiles.Concat(DATAFILES_BLACKLIST).ToArray();

                // Copy files, excluding the blacklist
                CopyFilesRecursive(dinfo, blacklist, tempProfilePath + $"{sep}files_to_copy");
            }

            // Export profile as JSON
            string jsonOutput = JsonConvert.SerializeObject(profile);
            File.WriteAllText(tempProfilePath + $"{sep}modmeta.json", jsonOutput);

            if (isVerboseOn)
                Console.WriteLine("DEBUG: Successfully wrote json file");

            // Compress temp folder to .zip
            if (File.Exists(output))
            {
                File.Delete(output);

                if (isVerboseOn)
                    Console.WriteLine($"DEBUG: {output} existed already, deletion successfull");
            }

            //try catch this. It could be that the path is read-only, that the path is too long, io error occured or something else
            try
            {
                ZipFile.CreateFromDirectory(tempProfilePath, output);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                AbortPatch();
                return;
            }

            if (isVerboseOn)
                Console.WriteLine($"DEBUG: Zipping to {output} successfull");

            // Delete temp folder
            Directory.Delete(tempPath, true);

            if (isVerboseOn)
                Console.WriteLine($"DEBUG: {tempPath} successfully deleted.");

            Console.WriteLine("Mod package was created!");

            #endregion patching

        }


        static parameterValues ReturnParameterValue(string argument)
        {
            int addNumber = argument[1] == '-' ? 2 : 1;
            int length = Enum.GetNames(typeof(parameterValues)).Length*2-2;
            for(int i = addNumber-1; i<length; i = i + 2)
            {
                if (argument == viableParameters[i])
                    return (parameterValues)((i / 2) + 1);
            }
            return parameterValues.invalid;
        }

        static private void AbortPatch()
        {
            // Unload files
            originalPath = "";
            modPath = "";
            apkPath = "";

            // Remove temp directory
            if (Directory.Exists(Path.GetTempPath() + $"{sep}AM2RModPacker"))
            {
                Directory.Delete(Path.GetTempPath() + $"{sep}AM2RModPacker", true);
            }
        }


        static private string CalculateMD5(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        static private void CopyFilesRecursive(DirectoryInfo source, string[] blacklist, string destination)
        {
            if (isVerboseOn)
                Console.WriteLine($"DEBUG: copying files from {source} to {destination}");


            foreach (FileInfo file in source.GetFiles())
            {
                if (!blacklist.Contains(file.Name))
                {
                    file.CopyTo(destination + $"{sep}" + file.Name);
                }
            }

            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                string newDir = Directory.CreateDirectory(destination + $"{sep}" + dir.Name).FullName;
                CopyFilesRecursive(dir, blacklist, newDir);
            }
        }

        static private int CreatePatch(string original, string modified, string output)
        {
            string processName =  isCurrentOSWindows ? localPath + $"{sep}utilities{sep}xdelta{sep}xdelta3.exe" : "xdelta3";

            // Specify process start info
            ProcessStartInfo parameters = new ProcessStartInfo
            {
                FileName = processName,
                WorkingDirectory = localPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "-f -e -s \"" + original + "\" \"" + modified + "\" \"" + output + "\""
            };

            if (isVerboseOn)
            {
                Console.WriteLine($"DEBUG: IsCurrentOSWindows? -> {isCurrentOSWindows}");
                Console.WriteLine($"DEBUG: Trying to execute \"{parameters.FileName} {parameters.Arguments}\"");
            }

            //we only check for this on windows, since on other platforms, we just call xdelta via command.
            if (isCurrentOSWindows && !File.Exists(parameters.FileName))
            {
                Console.WriteLine("The file could not be found! Make sure, that the utilities are in the current directory!");
                return -1;
            }

            // Launch process and wait for exit. using statement automatically disposes the object for us!
            using (Process proc = new Process { StartInfo = parameters })
            {
                //on *nix systems and on very weird edge cases, this could still fail, so we try-catch this.
                try
                {
                    proc.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Xdelta3 could not be found. Make sure, that the utilities exist or that you have the xdelta3 package installed.");
                    return -1;
                }
                proc.WaitForExit();
            }
            return 0;
        }


        static void DisplayHelp()
        {
            Console.WriteLine("A mod packaging toolchain for AM2RLauncher mods.\n");
            Console.WriteLine("Usage:");
            Console.WriteLine("AM2RModPackerConsole --name NAME --author AUTHOR --original ORIGINALPATH --mod MODPATH [--custommusic] [--savedata] [--yoyo]\n");
            Console.WriteLine("Descrption:");
            Console.WriteLine("-n, --name");
            Console.WriteLine("\t The name of the Mod\n");
            Console.WriteLine("-a, --author");
            Console.WriteLine("\t The name of the author\n");
            Console.WriteLine("-o, --original");
            Console.WriteLine("\t The path to the AM2R_11.zip file\n");
            Console.WriteLine("-m, --mod");
            Console.WriteLine("\t The path to your custom AM2R.zip\n");
            Console.WriteLine("-c, --custommusic");
            Console.WriteLine("\t Specify if your mod uses custom music\n");
            Console.WriteLine("-s, --savedata");
            Console.WriteLine("\t Specify if your mod uses a custom savedata folder\n");
            Console.WriteLine("-y, --yoyo");
            Console.WriteLine("\t Use the YoYo compiler instead of the normal one\n");
            Console.WriteLine("-h, --help");
            Console.WriteLine("\t Displays this help and exit\n");
        }
    }
}
