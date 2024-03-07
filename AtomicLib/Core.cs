using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using AtomicLib.XML;

namespace AtomicLib;

public enum ProfileOperatingSystems
{
    Unknown,
    Windows,
    Linux,
    Mac,
    Android
}

/// <summary>
/// An enum, that has possible return codes for <see cref="Core.CheckIfZipIsAM2R11"/>.
/// </summary>
public enum IsZipAM2R11ReturnCodes
{
    Successful,
    MissingOrInvalidAM2RExe,
    MissingOrInvalidD3DX943Dll,
    MissingOrInvalidDataWin,
    GameIsInASubfolder
}

// TODO: documentation

public static class Core
{
    public const string Version = "2.1.0";
    private static readonly string localPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
    
    public static void CreateModPack(ModCreationInfo modInfo, string output)
    {
        if (modInfo is null)
            throw new NullReferenceException(nameof(modInfo));
        if (modInfo.Profile is null)
            throw new NullReferenceException(nameof(modInfo.Profile));
        if (!File.Exists(modInfo.AM2R11Path))
            throw new FileNotFoundException("AM2R_11 file path could not be found!");
        if (modInfo.Profile.SupportsAndroid && !File.Exists(modInfo.ApkModPath))
            throw new FileNotFoundException("Android is marked as supported, but the APK path (" + modInfo.ApkModPath + ") could not be found!");
        
        ProfileOperatingSystems profileOS;
        if (!Enum.TryParse(modInfo.Profile.OperatingSystem, out profileOS))
            profileOS = ProfileOperatingSystems.Unknown;
        string modZipPath = profileOS switch
        {
            ProfileOperatingSystems.Windows => modInfo.WindowsModPath,
            ProfileOperatingSystems.Linux => modInfo.LinuxModPath,
            ProfileOperatingSystems.Mac => modInfo.MacModPath,
            _ => throw new NotSupportedException("The current operating system is not supported!")
        };

        if (!File.Exists(modZipPath))
            throw new FileNotFoundException("The file path (" + modZipPath + ") for the OS (" + modInfo.Profile.OperatingSystem + ") could not be found!");
                
        // Cleanup in case of previous errors
        if (Directory.Exists($"{Path.GetTempPath()}/Atomic"))
            Directory.Delete($"{Path.GetTempPath()}/Atomic", true);

        // Create temp work folders
        string tempPath = Directory.CreateDirectory($"{Path.GetTempPath()}/Atomic").FullName;
        string tempOriginalPath = Directory.CreateDirectory($"{tempPath}/original").FullName;
        string tempModPath = Directory.CreateDirectory($"{tempPath}/mod").FullName;
        string tempProfilePath = Directory.CreateDirectory($"{tempPath}/profile").FullName;

        // Extract 1.1 and modded AM2R to their own directories in temp work
        // We *probably* should check for 1.1 validity before extracting, *HOWEVER* that makes it kinda difficult to test against.
        ZipFile.ExtractToDirectory(modInfo.AM2R11Path, tempOriginalPath);
        ZipFile.ExtractToDirectory(modZipPath, tempModPath);

        // There once was a workaround here to work with Linux mods built with GMS1.4, however since then, GMS broke even more and is now seemingly unable to built for Linux.
        
        // Create AM2R.exe and data.win patches
        switch (profileOS)
        {
            case ProfileOperatingSystems.Windows:
                if (modInfo.Profile.UsesYYC)
                {
                    CreatePatch($"{tempOriginalPath}/data.win", $"{tempModPath}/AM2R.exe", $"{tempProfilePath}/AM2R.xdelta");
                }
                else
                {
                    CreatePatch($"{tempOriginalPath}/data.win", $"{tempModPath}/data.win", $"{tempProfilePath}/data.xdelta");
                    CreatePatch($"{tempOriginalPath}/AM2R.exe", $"{tempModPath}/AM2R.exe", $"{tempProfilePath}/AM2R.xdelta");
                }
                break;
            
            case ProfileOperatingSystems.Linux:
                string runnerName = File.Exists($"{tempModPath}/AM2R") ? "AM2R" : "runner";
                CreatePatch($"{tempOriginalPath}/data.win", $"{tempModPath}/assets/game.unx", $"{tempProfilePath}/game.xdelta");
                CreatePatch($"{tempOriginalPath}/AM2R.exe", $"{tempModPath}/{runnerName}", $"{tempProfilePath}/AM2R.xdelta");
                break;
            
            case ProfileOperatingSystems.Mac:
                CreatePatch($"{tempOriginalPath}/data.win", $"{tempModPath}/AM2R.app/Contents/Resources/game.ios", $"{tempProfilePath}/game.xdelta");
                CreatePatch($"{tempOriginalPath}/AM2R.exe", $"{tempModPath}/AM2R.app/Contents/MacOS/Mac_Runner", $"{tempProfilePath}/AM2R.xdelta");

                // Copy plist over for custom title name
                File.Copy($"{tempModPath}/AM2R.app/Contents/Info.plist", $"{tempProfilePath}/Info.plist");
                break;
        }
        
        // Create game.droid patch and wrapper if Android is supported
        if (modInfo.Profile.SupportsAndroid)
        {
            string tempAndroid = Directory.CreateDirectory($"{tempPath}/android").FullName;
            
            // Extract APK first in order to create patch from the data.win 
            // java -jar apktool.jar d "AM2RWrapper_old.apk"
            RunJavaJar($"\"{localPath}/utilities/android/apktool.jar\" d -f -o \"{tempAndroid}\" \"{modInfo.ApkModPath}\"");
            
            // Create game.droid patch
            CreatePatch($"{tempOriginalPath}/data.win", $"{tempAndroid}/assets/game.droid", $"{tempProfilePath}/droid.xdelta");

            // Delete excess files in APK, so we can use it as a bare-minimum wrapper
            // Create whitelist
            string[] whitelist = { "splash.png", "portrait_splash.png" };
            // Get directory
            var androidAssets = new DirectoryInfo($"{tempAndroid}/assets");
            // Delete files and folders
            foreach (var file in androidAssets.GetFiles())
            {
                // Not really sure why it's checked like this, but AM2R.ini is a file necessary to boot for YYC 
                if (file.Name.EndsWith(".ini") && file.Name != "modifiers.ini")
                    File.Copy(file.FullName, $"{tempProfilePath}/AM2R.ini", true);

                if (!whitelist.Contains(file.Name))
                    File.Delete(file.FullName);
            }
            foreach (var dir in androidAssets.GetDirectories())
                Directory.Delete(dir.FullName, true);

            // And now we create the wrapper from it
            // Process startInfo
            // java -jar apktool.jar b "AM2RWrapper_old" -o "AM2RWrapper.apk"
            RunJavaJar($"\"{localPath}/utilities/android/apktool.jar\" b -f \"{tempAndroid}\" -o \"{tempProfilePath}/AM2RWrapper.apk\"");
            
            string tempAndroidWrapperPath = $"{tempProfilePath}/android";
            Directory.CreateDirectory(tempAndroidWrapperPath);

            File.Move($"{tempProfilePath}/AM2RWrapper.apk", $"{tempAndroidWrapperPath}/AM2RWrapper.apk");
            if (File.Exists($"{tempProfilePath}/AM2R.ini"))
                File.Move($"{tempProfilePath}/AM2R.ini", $"{tempAndroidWrapperPath}/AM2R.ini");
        }

        // Copy datafiles and exclude .ogg if custom music is not selected
        var gameAssetDir = new DirectoryInfo(tempModPath);
        if (profileOS == ProfileOperatingSystems.Linux)
            gameAssetDir = new DirectoryInfo($"{tempModPath}/assets");
        else if (profileOS == ProfileOperatingSystems.Mac)
            gameAssetDir = new DirectoryInfo($"{tempModPath}/AM2R.app/Contents/Resources");

        Directory.CreateDirectory($"{tempProfilePath}/files_to_copy");
        string[] datafilesBlacklist = { "data.win", "AM2R.exe", "D3DX9_43.dll", "game.unx", "game.ios" };

        if (modInfo.Profile.UsesCustomMusic)
        {
            // Copy all files, excluding the blacklist
            CopyFilesRecursive(gameAssetDir, datafilesBlacklist, $"{tempProfilePath}/files_to_copy");
        }
        else
        {
            // Get list of 1.1's music files
            string[] musFiles = Directory.GetFiles(tempOriginalPath, "*.ogg").Select(file => Path.GetFileName(file)).ToArray();
            // Since on Unix our songs are in lowercase and we want to compare them later, we need to adjust for it here
            if (profileOS == ProfileOperatingSystems.Linux || profileOS == ProfileOperatingSystems.Mac)
                musFiles = musFiles.Select(f => f.ToLower()).ToArray();
            // Combine musFiles with the known datafiles for a blacklist
            string[] blacklist = musFiles.Concat(datafilesBlacklist).ToArray();
            // Copy files, excluding the blacklist
            CopyFilesRecursive(gameAssetDir, blacklist, $"{tempProfilePath}/files_to_copy");
        }

        // Export profile as XML
        string xmlOutput = Serializer.Serialize<ModProfileXML>(modInfo.Profile);
        File.WriteAllText($"{tempProfilePath}/profile.xml", xmlOutput);

        // Compress temp folder to .zip
        if (File.Exists(output))
            File.Delete(output);

        ZipFile.CreateFromDirectory(tempProfilePath, output);

        // Delete temp folder
        Directory.Delete(tempPath, true);
    }
    
    public static void CreatePatch(string original, string modified, string output)
    {
        // Specify process start info
        var parameters = new ProcessStartInfo
        {
            FileName = OS.IsWindows ? localPath + "/utilities/xdelta/xdelta3.exe" : "xdelta3",
            WorkingDirectory = localPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = "-f -e -s \"" + original + "\" \"" + modified + "\" \"" + output + "\""
        };

        // Launch process and wait for exit.
        try
        {
            using var proc = new Process { StartInfo = parameters };
            proc.Start();
            proc.WaitForExit();
        }
        catch (Win32Exception)
        {
            throw new IOException("Xdelta3 could not be found! For Windows, make sure that the utilities folder exists, for other OS make sure it is installed and in PATH.");
        }
    }
    
    public static void RunJavaJar(string arguments = null, string workingDirectory = null)
    {
        workingDirectory ??= Directory.GetCurrentDirectory();
        string proc = "",
               javaArgs = "";

        if (OS.IsWindows)
        {
            proc = "cmd";
            javaArgs = "/C java -jar";
        }
        else if (OS.IsUnix)
        {
            proc = "java";
            javaArgs = "-jar";
        }

        ProcessStartInfo jarStart = new ProcessStartInfo
        {
            FileName = proc,
            Arguments = $"{javaArgs} {arguments}",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process jarProcess = new Process
        {
            StartInfo = jarStart
        };

        try
        {
            jarProcess.Start();
            jarProcess.WaitForExit();
        }
        catch
        {
            throw new IOException("Java could not be found! Make sure it is installed and in PATH.");
        }
        
    }
    
    // Taken from AM2RLauncher
    /// <summary>
    /// Checks if a Zip file is a valid AM2R_1.1 zip.
    /// </summary>
    /// <param name="zipPath">Full Path to the Zip file to validate.</param>
    /// <returns><see cref="IsZipAM2R11ReturnCodes"/> detailing the result</returns>
    public static IsZipAM2R11ReturnCodes CheckIfZipIsAM2R11(string zipPath)
    {
        const string d3dHash = "86e39e9161c3d930d93822f1563c280d";
        const string dataWinHash = "f2b84fe5ba64cb64e284be1066ca08ee";
        const string am2rHash = "15253f7a66d6ea3feef004ebbee9b438";
        string tmpPath = Path.GetTempPath() + "/" + Path.GetFileNameWithoutExtension(zipPath);

        // Clean up in case folder exists already
        if (Directory.Exists(tmpPath))
            Directory.Delete(tmpPath, true);
        Directory.CreateDirectory(tmpPath);

        // Open archive
        ZipArchive am2rZip = ZipFile.OpenRead(zipPath);
        
        // Check if exe exists anywhere
        ZipArchiveEntry am2rExe = am2rZip.Entries.FirstOrDefault(x => x.FullName.Contains("AM2R.exe"));
        if (am2rExe == null)
            return IsZipAM2R11ReturnCodes.MissingOrInvalidAM2RExe;

        // Check if it's not in a subfolder. if it'd be in a subfolder, fullname would be "folder/AM2R.exe"
        if (am2rExe.FullName != "AM2R.exe")
            return IsZipAM2R11ReturnCodes.GameIsInASubfolder;

        // Check validity
        am2rExe.ExtractToFile($"{tmpPath}/{am2rExe.FullName}");
        if (CalculateMD5($"{tmpPath}/{am2rExe.FullName}") != am2rHash)
            return IsZipAM2R11ReturnCodes.MissingOrInvalidAM2RExe;


        // Check if data.win exists / is valid
        ZipArchiveEntry dataWin = am2rZip.Entries.FirstOrDefault(x => x.FullName == "data.win");
        if (dataWin == null)
            return IsZipAM2R11ReturnCodes.MissingOrInvalidDataWin;

        dataWin.ExtractToFile($"{tmpPath}/{dataWin.FullName}");
        if (CalculateMD5($"{tmpPath}/{dataWin.FullName}") != dataWinHash)
            return IsZipAM2R11ReturnCodes.MissingOrInvalidDataWin;


        // Check if d3d.dll exists / is valid
        ZipArchiveEntry d3dx = am2rZip.Entries.FirstOrDefault(x => x.FullName == "D3DX9_43.dll");
        if (d3dx == null)
            return IsZipAM2R11ReturnCodes.MissingOrInvalidD3DX943Dll;

        d3dx.ExtractToFile($"{tmpPath}/{d3dx.FullName}");
        if (CalculateMD5($"{tmpPath}/{d3dx.FullName}") != d3dHash)
            return IsZipAM2R11ReturnCodes.MissingOrInvalidD3DX943Dll;
        
        // Clean up
        Directory.Delete(tmpPath, true);

        // If we didn't exit before, everything is fine
        return IsZipAM2R11ReturnCodes.Successful;
    }
    
    private static string CalculateMD5(string filename)
    {
        using var stream = File.OpenRead(filename);
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
    
    private static void CopyFilesRecursive(DirectoryInfo source, string[] blacklist, string destination)
    {
        foreach (var file in source.GetFiles())
        {
            if (!blacklist.Contains(file.Name, StringComparer.OrdinalIgnoreCase))
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