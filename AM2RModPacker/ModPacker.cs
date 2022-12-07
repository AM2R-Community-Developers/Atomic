using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using AM2RModPackerLib;
using AM2RModPackerLib.XML;
using Eto.Forms;

namespace AM2RModPacker;

public partial class ModPacker : Form
{
    private static readonly string version = Core.Version;
    private const string originalMD5 = "f2b84fe5ba64cb64e284be1066ca08ee";
    private bool isOriginalLoaded, isModLoaded, isApkLoaded, isLinuxLoaded;
    // TODO: do not get current directory, wont end well
    private readonly string localPath = Directory.GetCurrentDirectory();
    private string originalPath, modPath, apkPath, linuxPath;
    private readonly string[] DATAFILES_BLACKLIST = { "data.win", "AM2R.exe", "D3DX9_43.dll", "game.unx" };
    private string saveFilePath;
    private readonly ModProfileXML profile;

    private readonly FileFilter zipFileFilter = new FileFilter("zip archives (*.zip)", ".zip");
    private readonly FileFilter apkFileFilter = new FileFilter("Android application packages (*.apk)", ".apk");

    #region Eto events
    private void CustomSaveCheckBoxChecked_Changed(object sender, EventArgs e)
    {
        customSaveButton.Enabled = customSaveCheckBox.Checked.Value;
        customSaveTextBox.Enabled = customSaveCheckBox.Checked.Value;
        UpdateCreateButton();
    }
    
    //TODO: go through this method
    private void CustomSaveDataButton_Click(object sender, EventArgs e)
    {
        bool wasSuccessful = false;
        // TODO: make sure that the validation works on other platforms too
        var saveRegex = new Regex(@"C:\\Users\\.*\\AppData\\Local\\"); //this is to ensure, that the save directory is valid. so far, this is only important for windows


        var dialog = new SelectFolderDialog();
        // currently not implemented in eto
        //dialog.InitialDirectory = Environment.GetEnvironmentVariable("LocalAppData");
        while (!wasSuccessful)
        {
            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                var match = saveRegex.Match(dialog.Directory);
                if (match.Success == false)
                {
                    MessageBox.Show("Invalid Save Directory! Please choose one in %LocalAppData%");
                }
                else
                {
                    wasSuccessful = true;
                    saveFilePath = dialog.Directory.Replace(match.Value, "%localappdata%/");
                    saveFilePath = saveFilePath.Replace("\\", "/"); // if we don't do this, custom save locations are going to fail on Linux
                    // if someone has a custom save path inside of am2r and creates these whithin game maker, they will always be lower case
                    // we need to adjust them here to lowercase as well, as otherwise launcher gets problems on nix systems
                    const string vanillaPrefix = "%localappdata%/AM2R/";
                    if (saveFilePath.Contains(vanillaPrefix))
                        saveFilePath = vanillaPrefix + saveFilePath.Substring(vanillaPrefix.Length).ToLower();
                }
            }
            else
            {
                wasSuccessful = true;
                saveFilePath = null;
            }
        }
        customSaveTextBox.Text = saveFilePath;
    }

    private void ApkCheckBoxCheckedChanged(object sender, EventArgs e)
    {
        apkButton.Enabled = apkCheckBox.Checked.Value;
        UpdateCreateButton();
    }
    
    private void ApkButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded AM2R APK
        (isApkLoaded, apkPath) = SelectFile("Please select your custom AM2R .apk", apkFileFilter);
        apkLabel.Visible = isApkLoaded;
        UpdateCreateButton();
    }

    private void LinuxCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        linuxButton.Enabled = linuxCheckBox.Checked.Value;
        UpdateCreateButton();
    }

    private void LinuxButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded Linux .zip
        (isLinuxLoaded, linuxPath) = SelectFile("Please select your custom Linux AM2R .zip", zipFileFilter);
        linuxLabel.Visible = isLinuxLoaded;
        UpdateCreateButton();
    }

    private void OriginalZipButton_Click(object sender, EventArgs e)
    {
        // Open window to select AM2R 1.1
        (isOriginalLoaded, originalPath) = SelectFile("Please select AM2R_11.zip", zipFileFilter);
        originalZipLabel.Visible = isOriginalLoaded;
        UpdateCreateButton();
    }

    private void ModZipButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded AM2R
        (isModLoaded, modPath) = SelectFile("Please select your custom AM2R .zip", zipFileFilter);
        modZipLabel.Visible = isModLoaded;
        UpdateCreateButton();
    }
    
    private void CreateButton_Click(object sender, EventArgs e)
    {
        if (nameTextBox.Text == "" || authorTextBox.Text == "" || versionTextBox.Text == "")
        {
            MessageBox.Show("Text field missing! Mod packaging aborted.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            return;
        }

        if (Path.GetInvalidFileNameChars().Any(nameTextBox.Text.Contains))
        {
            MessageBox.Show("Name contains invalid characters! These characters are not allowed:\n" + String.Join("\n", Path.GetInvalidFileNameChars()));
            return;
        }

        createLabel.Visible = true;
        createLabel.Text = "Packaging mod(s)... This could take a while!";
        
        string output;

        // TODO: make windows optional
        using (var saveFile = new SaveFileDialog { Title = "Save Windows mod profile", Filters = { zipFileFilter } })
        {
            if (saveFile.ShowDialog(this) == DialogResult.Ok)
                output = saveFile.FileName;
            else
            {
                createLabel.Text = "Mod packaging aborted!";
                return;
            }
        }
        LoadProfileParameters(ProfileOperatingSystems.Windows);
        CreateModPack(profile, modPath, output);

        if (linuxCheckBox.Checked.Value)
        {
            using (var saveFile = new SaveFileDialog { Title = "Save Linux mod profile", Filters = { zipFileFilter } })
            {
                if (saveFile.ShowDialog(this) == DialogResult.Ok)
                    output = saveFile.FileName;
                else
                {
                    createLabel.Text = "Mod packaging aborted!";
                    return;
                }
            }
            LoadProfileParameters(ProfileOperatingSystems.Linux);
            CreateModPack(profile, linuxPath, output);
        }
        //TODO: mac

        createLabel.Text = "Mod package(s) created!";
    }
    #endregion

    // TODO: go over thhis
    private void CreateModPack( ModProfileXML profile, string input, string output)
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
            MessageBox.Show("Could not create temp directory! Please run the application with administrator rights.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            AbortPatch();
            return;
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
                // Show error box
                MessageBox.Show("1.1 data.win does not meet MD5 checksum! Mod packaging aborted.\n1.1 MD5: " + originalMD5 + "\nYour MD5: " + newMD5, "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                AbortPatch();
                return;
            }
        }
        catch (FileNotFoundException)
        {
            // Show error message
            MessageBox.Show("data.win not found! Are you sure you selected AM2R 1.1? Mod packaging aborted.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            AbortPatch();
            return;
        }

        // Create AM2R.exe and data.win patches
        if (profile.OperatingSystem == "Windows")
        {
            if (!File.Exists(tempModPath + "/AM2R.exe"))
            {
                var result = MessageBox.Show("Modded game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                    AbortPatch();
            }

            if (File.Exists(tempModPath + "profile.xml"))
            {
                var result = MessageBox.Show("profile.xml found. This file is used by the AM2RLauncher to determine profile stats and its inclusion may make the profile uninstallable. Are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                    AbortPatch();
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
            {
                var result = MessageBox.Show("Modded Linux game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                    AbortPatch();
            }

            if (File.Exists(tempModPath + "profile.xml"))
            {
                var result = MessageBox.Show("profile.xml found. This file is used by the AM2RLauncher to determine profile stats and its inclusion may make the profile uninstallable. Are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                    AbortPatch();
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
    }

    private void LoadProfileParameters(ProfileOperatingSystems operatingSystem)
    {
        profile.Name = nameTextBox.Text;
        profile.Author = authorTextBox.Text;
        profile.Version = versionTextBox.Text;
        profile.UsesCustomMusic = musicCheckBox.Checked.Value;
        profile.UsesYYC = yycCheckBox.Checked.Value;
        profile.Android = apkCheckBox.Checked.Value;
        profile.ProfileNotes = modNotesTextBox.Text;
        profile.OperatingSystem = operatingSystem.ToString();
        if (customSaveCheckBox.Checked.Value && customSaveTextBox.Text != "")
            profile.SaveLocation = customSaveTextBox.Text;
        else
            profile.SaveLocation = "%localappdata%/AM2R";
        if (operatingSystem == ProfileOperatingSystems.Linux)
            profile.SaveLocation = profile.SaveLocation.Replace("%localappdata%", "~/.config");
    }

    private void AbortPatch()
    {
        // Unload files
        isOriginalLoaded = false;
        isModLoaded = false;
        isApkLoaded = false;
        isLinuxLoaded = false;
        originalPath = "";
        modPath = "";
        apkPath = "";
        linuxPath = "";
        saveFilePath = null;

        // Set labels
        createLabel.Text = "Mod packaging aborted!";
        originalZipLabel.Visible = false;
        modZipLabel.Visible = false;
        apkLabel.Visible = false;
        linuxLabel.Visible = false;

        // Remove temp directory
        if (Directory.Exists(Path.GetTempPath() + "/AM2RModPacker"))
            Directory.Delete(Path.GetTempPath() + "/AM2RModPacker", true);
    }



    private void UpdateCreateButton()
    {
        if (isOriginalLoaded &&                                                  // AM2R_11 zip must be provided
            isModLoaded &&                                                       // Modded zip must be provided
            (!apkCheckBox.Checked.Value || isApkLoaded) &&                       // either APK is disabled OR APK is provided
            (!linuxCheckBox.Checked.Value || isLinuxLoaded) &&                   // either Linux is disabled OR linux is provided
            (!customSaveCheckBox.Checked.Value || customSaveTextBox.Text != "")) // either custom saves are disabled OR custom save is provided
            createButton.Enabled = true;
        else
            createButton.Enabled = false;
    }
    
    //todo: make this part of interface
    private (bool, string) SelectFile(string title, FileFilter filter)
    {
        using var fileFinder = new OpenFileDialog { Filters = { filter } };
        fileFinder.Title = title;
        fileFinder.CurrentFilter = fileFinder.Filters.First();
        fileFinder.CheckFileExists = true;

        if (fileFinder.ShowDialog(this) != DialogResult.Ok)
            return (false, "");

        string location = fileFinder.FileName;
        return (true, location);
    }
}