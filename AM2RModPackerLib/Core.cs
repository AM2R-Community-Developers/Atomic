using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using AM2RModPackerLib.XML;

namespace AM2RModPackerLib;

public enum ProfileOperatingSystems
{
    Windows,
    Linux,
    Mac
}

public static class Core
{
    public static readonly string Version = "2.0.3";
    private const string originalMD5 = "f2b84fe5ba64cb64e284be1066ca08ee";
    private static readonly string[] DATAFILES_BLACKLIST = { "data.win", "AM2R.exe", "D3DX9_43.dll", "game.unx" };
    private static readonly string localPath = Directory.GetCurrentDirectory();
    
    // TODO: go over thhis and clean
    public static (bool, string) CreateModPack(ModProfileXML profile, string input, string originalPath, string apkPath, string output)
    {
        // Cleanup in case of previous errors
        if (Directory.Exists(Path.GetTempPath() + "/AM2RModPacker"))
            Directory.Delete(Path.GetTempPath() + "/AM2RModPacker", true);

        // Create temp work folders
        string tempPath,
               tempOriginalPath,
               tempModPath,
               tempProfilePath;

        // We might not have permission to access to the temp directory, so we need to catch the exception.
        try
        {
            tempPath = Directory.CreateDirectory(Path.GetTempPath() + "/AM2RModPacker").FullName;
            tempOriginalPath = Directory.CreateDirectory(tempPath + "/original").FullName;
            tempModPath = Directory.CreateDirectory(tempPath + "/mod").FullName;
            tempProfilePath = Directory.CreateDirectory(tempPath + "/profile").FullName;
        }
        catch (System.Security.SecurityException)
        {
            return (false, "Could not create temp directory! Please run the application with administrator rights.");
        }

        // Extract 1.1 and modded AM2R to their own directories in temp work
        ZipFile.ExtractToDirectory(originalPath, tempOriginalPath);
        ZipFile.ExtractToDirectory(input, tempModPath);

        if (Directory.Exists(tempModPath + "/AM2R"))
            tempModPath += "/AM2R";

        // Verify 1.1 with an MD5. If it does not match, exit cleanly and provide a warning window.
        try
        {
            // TODO: dont. do what launcher does
            string newMD5 = Core.CalculateMD5(tempOriginalPath + "/data.win");

            if (newMD5 != originalMD5)
            {
                return (false, "1.1 data.win does not meet MD5 checksum! Mod packaging aborted.\n1.1 MD5: " + originalMD5 + "\nYour MD5: " + newMD5);
            }
        }
        catch (FileNotFoundException)
        {
            return (false, "data.win not found! Are you sure you selected AM2R 1.1? Mod packaging aborted.");
        }

        // Create AM2R.exe and data.win patches
        if (profile.OperatingSystem == "Windows")
        {
            if (!File.Exists(tempModPath + "/AM2R.exe"))
            { //TODO: put this onto the outer method
                /*
                var result = MessageBox.Show("Modded game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return (false, "");
                }*/
            }

            if (File.Exists(tempModPath + "profile.xml"))
            {
                //TODO: put this onto the outer method
                /*var result = MessageBox.Show("profile.xml found. This file is used by the AM2RLauncher to determine profile stats and its inclusion may make the profile uninstallable. Are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                return (false, "");
                }*/
            }

            if (profile.UsesYYC)
            {
                Core.CreatePatch(tempOriginalPath + "/data.win", tempModPath + "/AM2R.exe", tempProfilePath + "/AM2R.xdelta");
            }
            else
            {
                Core.CreatePatch(tempOriginalPath + "/data.win", tempModPath + "/data.win", tempProfilePath + "/data.xdelta");
                Core.CreatePatch(tempOriginalPath + "/AM2R.exe", tempModPath + "/AM2R.exe", tempProfilePath + "/AM2R.xdelta");
            }
        }
        else if (profile.OperatingSystem == "Linux")
        {
            string runnerName = File.Exists(tempModPath + "/" + "AM2R") ? "AM2R" : "runner";

            if (!File.Exists(tempModPath + "/" + runnerName))
            {//TODO: put this onto the outer method
                /*
                var result = MessageBox.Show("Modded Linux game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return (false, "");
                }*/
            }

            if (File.Exists(tempModPath + "profile.xml"))
            {
               //TODO: put this onto the outer method
                /* var result = MessageBox.Show("profile.xml found. This file is used by the AM2RLauncher to determine profile stats and its inclusion may make the profile uninstallable. Are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return (false, "");
                }*/
            }

            Core.CreatePatch(tempOriginalPath + "/data.win", tempModPath + "/assets/game.unx", tempProfilePath + "/game.xdelta");
            Core.CreatePatch(tempOriginalPath + "/AM2R.exe", tempModPath + "/" + runnerName, tempProfilePath + "/AM2R.xdelta");
        }
        // todo: mac

        // Create game.droid patch and wrapper if Android is supported
        if (profile.Android)
        {
            string tempAndroid = Directory.CreateDirectory(tempPath + "/android").FullName;

            // Extract APK 
            // - java -jar apktool.jar d "%~dp0AM2RWrapper_old.apk"

            // Process startInfo
            // TODO: cross platform
            var procStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = tempAndroid,
                Arguments = "/C java -jar \"" + localPath + "/utilities/android/apktool.jar\" d -f -o \"" + tempAndroid + "\" \"" + apkPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Run process
            using (var proc = new Process { StartInfo = procStartInfo })
            {
                proc.Start();
                proc.WaitForExit();
            }

            // Create game.droid patch
            Core.CreatePatch(tempOriginalPath + "/data.win", tempAndroid + "/assets/game.droid", tempProfilePath + "/droid.xdelta");

            // Delete excess files in APK

            // Create whitelist
            string[] whitelist = { "splash.png", "portrait_splash.png" };

            // Get directory
            var androidAssets = new DirectoryInfo(tempAndroid + "/assets");


            // Delete files
            foreach (var file in androidAssets.GetFiles())
            {
                if (file.Name.EndsWith(".ini") && file.Name != "modifiers.ini")
                {
                    if (File.Exists(tempProfilePath + "/AM2R.ini"))
                        // This shouldn't be a problem... normally...
                        File.Delete(tempProfilePath + "/AM2R.ini");
                    File.Copy(file.FullName, tempProfilePath + "/AM2R.ini");
                }

                if (!whitelist.Contains(file.Name))
                    File.Delete(file.FullName);
            }

            foreach (var dir in androidAssets.GetDirectories())
                Directory.Delete(dir.FullName, true);

            // Create wrapper

            // Process startInfo
            // - java -jar apktool.jar b "%~dp0AM2RWrapper_old" -o "%~dp0AM2RWrapper.apk"
            var procStartInfo2 = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = tempAndroid,
                Arguments = "/C java -jar \"" + localPath + "/utilities/android/apktool.jar\" b -f \"" + tempAndroid + "\" -o \"" + tempProfilePath + "/AM2RWrapper.apk\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Run process
            using (var proc = new Process { StartInfo = procStartInfo2 })
            {
                proc.Start();
                proc.WaitForExit();
            }

            string tempAndroidProfilePath = tempProfilePath + "/android";
            Directory.CreateDirectory(tempAndroidProfilePath);

            File.Move(tempProfilePath + "/AM2RWrapper.apk", tempAndroidProfilePath + "/AM2RWrapper.apk");
            if (File.Exists(tempProfilePath + "/AM2R.ini"))
                File.Move(tempProfilePath + "/AM2R.ini", tempAndroidProfilePath + "/AM2R.ini");
        }

        // Copy datafiles (exclude .ogg if custom music is not selected)

        var dirInfo = new DirectoryInfo(tempModPath);
        if (profile.OperatingSystem == "Linux")
            dirInfo = new DirectoryInfo(tempModPath + "/assets");

        Directory.CreateDirectory(tempProfilePath + "/files_to_copy");

        if (profile.UsesCustomMusic)
        {
            // Copy files, excluding the blacklist
            Core.CopyFilesRecursive(dirInfo, DATAFILES_BLACKLIST, tempProfilePath + "/files_to_copy");
        }
        else
        {
            // Get list of 1.1's music files
            string[] musFiles = Directory.GetFiles(tempOriginalPath, "*.ogg").Select(file => Path.GetFileName(file)).ToArray();

            if (profile.OperatingSystem == "Linux")
                musFiles = Directory.GetFiles(tempOriginalPath, "*.ogg").Select(file => Path.GetFileName(file).ToLower()).ToArray();


            // Combine musFiles with the known datafiles for a blacklist
            string[] blacklist = musFiles.Concat(DATAFILES_BLACKLIST).ToArray();

            // Copy files, excluding the blacklist
            Core.CopyFilesRecursive(dirInfo, blacklist, tempProfilePath + "/files_to_copy");
        }

        // Export profile as XML
        string xmlOutput = Serializer.Serialize<ModProfileXML>(profile);
        File.WriteAllText(tempProfilePath + "/profile.xml", xmlOutput);

        // Compress temp folder to .zip
        if (File.Exists(output))
            File.Delete(output);

        ZipFile.CreateFromDirectory(tempProfilePath, output);

        // Delete temp folder
        Directory.Delete(tempPath, true);
        return (true, "");
    }

    
    public static void CreatePatch(string original, string modified, string output)
    {
        // Specify process start info
        var parameters = new ProcessStartInfo
        {
            // TODO: deal with linux/mac 
            FileName = localPath + "/utilities/xdelta/xdelta3.exe",
            WorkingDirectory = localPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = "-f -e -s \"" + original + "\" \"" + modified + "\" \"" + output + "\""
        };

        // Launch process and wait for exit. using statement automatically disposes the object for us!
        using var proc = new Process { StartInfo = parameters };
        proc.Start();
        proc.WaitForExit();
    }
    
    public static string CalculateMD5(string filename)
    {
        using var stream = File.OpenRead(filename);
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
    
    public static void CopyFilesRecursive(DirectoryInfo source, string[] blacklist, string destination)
    {
        foreach (var file in source.GetFiles())
        {
            if (!blacklist.Contains(file.Name))
                file.CopyTo(destination + "/" + file.Name);
        }

        foreach (var dir in source.GetDirectories())
        {
            // Folders need to be lowercase, because GM only reads from lowercase names on *nix systems. Windows is case-insensitive so doesnt matter for them
            string newDir = Directory.CreateDirectory(destination + "/" + dir.Name.ToLower()).FullName;
            CopyFilesRecursive(dir, blacklist, newDir);
        }
    }
}