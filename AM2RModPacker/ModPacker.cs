using System;
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

    private ModCreationInfo modinfo = new ModCreationInfo();
    
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
    
    private void CustomSaveDataButton_Click(object sender, EventArgs e)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "\\\\"); // This is \ -> \\
        bool wasSuccessful = false;
        var winSaveRegex = new Regex(@"C:\\Users\\.*\\AppData\\Local\\"); //this is to ensure, that the save directory is valid. so far, this is only important for windows
        var linSaveRegex = new Regex($@"{home}/\.config/"); // GMS hardcodes save into ~/.config on linux 
        var macSaveRegex = new Regex($@"{home}/Library/Application Support/");

        var dialog = new SelectFolderDialog();
        string initialDir = "";
        if (OS.IsWindows)
            initialDir = Environment.GetEnvironmentVariable("LocalAppData");
        else if (OS.IsLinux)
            initialDir = home + "/.config";
        else if (OS.IsMac)
            initialDir = $@"{home}/Library/Application Support/";
        
        dialog.Directory = initialDir;
        while (!wasSuccessful)
        {
            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                Match match = Match.Empty;
                if (OS.IsWindows)
                    match = winSaveRegex.Match(dialog.Directory);
                else if (OS.IsLinux)
                    match = linSaveRegex.Match(dialog.Directory);
                else if (OS.IsMac)
                    match = macSaveRegex.Match(dialog.Directory);
                
                if (match.Success == false)
                    MessageBox.Show("Invalid Save Directory! Please consult the GameMaker: Studio 1.4 documentation for valid save directories!");
                else
                {
                    wasSuccessful = true;
                    string saveFilePath = dialog.Directory.Replace(match.Value, "%localappdata%/");
                    // If we don't do this, custom save locations are going to fail on Linux
                    if (OS.IsWindows) 
                        saveFilePath = saveFilePath.Replace("\\", "/");
                    
                    // On Mac, we need to adjust the path
                    if (OS.IsMac)
                        saveFilePath = saveFilePath.Replace("com.yoyogames.am2r", "AM2R");
                    
                    // if someone has a custom save path inside of am2r and creates these whithin game maker, they will always be lower case
                    // we need to adjust them here to lowercase as well, as otherwise launcher gets problems on nix systems
                    const string vanillaPrefix = "%localappdata%/AM2R/";
                    if (saveFilePath.Contains(vanillaPrefix))
                        saveFilePath = vanillaPrefix + saveFilePath.Substring(vanillaPrefix.Length).ToLower();

                    modinfo.saveFilePath = saveFilePath;
                }
            }
            else
            {
                wasSuccessful = true;
                modinfo.saveFilePath = null;
            }
        }
        customSaveTextBox.Text = modinfo.saveFilePath;
    }
    
    private void YYCCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        // Don't do anything if it's been disabled
        if (!yycCheckBox.Checked.Value)
            return;
        macCheckBox.Checked = false;
        macLabel.Visible = false;
        macButton.Enabled = false;
        modinfo.macPath = "";
    }

    private void WindowsCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        windowsButton.Enabled = windowsCheckBox.Checked.Value;
        // If it was disabled, clean the appropriate attributes
        if (!windowsCheckBox.Checked.Value)
        {
            windowsLabel.Visible = false;
            modinfo.windowsPath = "";
        }
        UpdateCreateButton();
    }
    
    private void WindowsButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded Linux .zip
        (_, modinfo.windowsPath) = SelectFile("Please select your custom Windows AM2R .zip", zipFileFilter);
        windowsLabel.Visible = modinfo.IsWindowsModLoaded;
        UpdateCreateButton();
    }
    
    private void ApkCheckBoxCheckedChanged(object sender, EventArgs e)
    {
        apkButton.Enabled = apkCheckBox.Checked.Value;
        // If it was disabled, clean the appropriate attributes
        if (!apkCheckBox.Checked.Value)
        {
            apkLabel.Visible = false;
            modinfo.apkPath = "";
        }
        UpdateCreateButton();
    }
    
    private void ApkButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded AM2R APK
        (_, modinfo.apkPath) = SelectFile("Please select your custom AM2R .apk", apkFileFilter);
        apkLabel.Visible = modinfo.IsApkModLoaded;
        UpdateCreateButton();
    }

    private void LinuxCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        linuxButton.Enabled = linuxCheckBox.Checked.Value;
        // If it was disabled, clean the appropriate attributes
        if (!linuxCheckBox.Checked.Value)
        {
            linuxLabel.Visible = false;
            modinfo.linuxPath = "";
        }
        UpdateCreateButton();
    }

    private void LinuxButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded Linux .zip
        (_, modinfo.linuxPath) = SelectFile("Please select your custom Linux AM2R .zip", zipFileFilter);
        linuxLabel.Visible = modinfo.IsLinuxModLoaded;
        UpdateCreateButton();
    }
    
    private void macCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        if (!yycCheckBox.Checked.Value)
        {
            macButton.Enabled = macCheckBox.Checked.Value;
            // If it was disabled, clean the appropriate attributes
            if (!macCheckBox.Checked.Value)
            {
                macLabel.Visible = false;
                modinfo.macPath = "";
            }
            UpdateCreateButton();
        }
        else if (macCheckBox.Checked.Value)
        {
            MessageBox.Show("YoYoCompiler isn't supported with Mac!", "Warning", MessageBoxButtons.OK, MessageBoxType.Warning);
            macCheckBox.Checked = false;
        }
    }
    
    private void macButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded Mac .zip
        (_, modinfo.macPath) = SelectFile("Please select your custom Mac AM2R .zip", zipFileFilter);
        macLabel.Visible = modinfo.IsMacModLoaded;
        UpdateCreateButton();
    }

    private void OriginalZipButton_Click(object sender, EventArgs e)
    {
        // Open window to select AM2R 1.1
        (_, modinfo.originalPath) = SelectFile("Please select AM2R_11.zip", zipFileFilter);
        originalZipLabel.Visible = modinfo.IsAM2R11Loaded;
        UpdateCreateButton();
    }

    private void CreateButton_Click(object sender, EventArgs e)
    {
        if (nameTextBox.Text == "" || authorTextBox.Text == "" || versionTextBox.Text == "")
        {
            MessageBox.Show("Mod name, author or version field missing! Mod packaging aborted.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            return;
        }

        if (Path.GetInvalidFileNameChars().Any(nameTextBox.Text.Contains))
        {
            MessageBox.Show("Name contains invalid characters! These characters are not allowed:\n" + String.Join("\n", Path.GetInvalidFileNameChars()));
            return;
        }
        
        // Verify 1.1
        var result11 = Core.CheckIfZipIsAM2R11(modinfo.originalPath);
        if (result11 != IsZipAM2R11ReturnCodes.Successful)
        {
            MessageBox.Show("AM2R 1.1 zip is invalid! Error code: " + result11);
            AbortPatch();
            return;
        }

        createLabel.Visible = true;
        createLabel.Text = "Packaging mod(s)... This could take a while!";

        bool PromptAndSaveOSMod(ProfileOperatingSystems os)
        {
            string modZipPath = os switch
            {
                ProfileOperatingSystems.Windows => modinfo.windowsPath,
                ProfileOperatingSystems.Linux => modinfo.linuxPath,
                ProfileOperatingSystems.Mac => modinfo.macPath,
                _ => null
            };
            string output;
            
            using (var saveFile = new SaveFileDialog { Title = $"Save {os.ToString()} mod profile", Filters = { zipFileFilter } })
            {
                if (saveFile.ShowDialog(this) == DialogResult.Ok)
                    output = saveFile.FileName;
                else
                {
                    createLabel.Text = "Mod packaging aborted!";
                    return false;
                }
            }
            // Some filepickers don't automatically set the file extension
            if (!output.ToLower().EndsWith(".zip"))
                output += ".zip";
            LoadProfileParameters(os);
            try
            {
                Core.CreateModPack(profile, modinfo.originalPath, modZipPath, modinfo.apkPath, output);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                AbortPatch();
                return false;
            }
            return true;
        }

        bool CheckForProfileXML(ZipArchive zipfile)
        {
            if (zipfile.Entries.All(f => f.Name != "profile.xml"))
                return true;
            var result = MessageBox.Show("profile.xml found. This file is used by the AM2RLauncher to determine profile stats and its inclusion may make the profile uninstallable. Are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
            if (result == DialogResult.Yes)
                return true;
            AbortPatch();
            return false;
        }

        if (windowsCheckBox.Checked.Value)
        {
            var windowsZip = ZipFile.Open(modinfo.windowsPath, ZipArchiveMode.Read);
            if (windowsZip.Entries.All(f => f.FullName != "AM2R.exe"))
            {                
                var result = MessageBox.Show("Modded game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return;
                }
            }

            if (!CheckForProfileXML(windowsZip))
                return;

            if (!PromptAndSaveOSMod(ProfileOperatingSystems.Windows))
                return;
        }

        if (linuxCheckBox.Checked.Value)
        {
            var linuxZip = ZipFile.Open(modinfo.linuxPath, ZipArchiveMode.Read);
            if (linuxZip.Entries.All(f => f.FullName != "AM2R") && linuxZip.Entries.All(f => f.FullName != "runner"))
            { 
                var result = MessageBox.Show("Modded Linux game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return;
                }
            }

            if (!CheckForProfileXML(linuxZip))
                return;

            if (!PromptAndSaveOSMod(ProfileOperatingSystems.Linux))
                return;
        }
        if (macCheckBox.Checked.Value)
        {
            var macZip = ZipFile.Open(modinfo.macPath, ZipArchiveMode.Read);
            if (macZip.Entries.All(f => f.Name != "AM2R.app/Contents/MacOS/Mac_Runner"))
            {
                var result = MessageBox.Show("Modded Mac game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                    AbortPatch();
            }

            if (!CheckForProfileXML(macZip))
                return;

            if (!PromptAndSaveOSMod(ProfileOperatingSystems.Mac))
                return;
        }
        createLabel.Text = "Mod package(s) created!";
    }
    #endregion
    
    private void LoadProfileParameters(ProfileOperatingSystems operatingSystem)
    {
        profile.Name = nameTextBox.Text;
        profile.Author = authorTextBox.Text;
        profile.Version = versionTextBox.Text;
        profile.UsesCustomMusic = musicCheckBox.Checked.Value;
        profile.UsesYYC = yycCheckBox.Checked.Value;
        profile.SupportsAndroid = apkCheckBox.Checked.Value;
        profile.ProfileNotes = modNotesTextBox.Text;
        profile.OperatingSystem = operatingSystem.ToString();
        if (customSaveCheckBox.Checked.Value && customSaveTextBox.Text != "")
            profile.SaveLocation = customSaveTextBox.Text;
        else
            profile.SaveLocation = "%localappdata%/AM2R";
        if (operatingSystem == ProfileOperatingSystems.Linux)
            profile.SaveLocation = profile.SaveLocation.Replace("%localappdata%", "~/.config");
        else if (operatingSystem == ProfileOperatingSystems.Mac)
        {
            if (profile.SaveLocation.Contains("%localappdata%/AM2R"))
                profile.SaveLocation = profile.SaveLocation.Replace("%localappdata%/AM2R", "~/Library/Application Support/com.yoyogames.am2r");
            else
                profile.SaveLocation = "~/Library/Application Support/com.yoyogames." + new DirectoryInfo(profile.SaveLocation).Name.ToLower();
        }
    }

    private void AbortPatch()
    {
        // Set labels
        createLabel.Text = "Mod packaging aborted!";

        // Remove temp directory
        if (Directory.Exists(Path.GetTempPath() + "/AM2RModPacker"))
            Directory.Delete(Path.GetTempPath() + "/AM2RModPacker", true);
    }
    
    private void UpdateCreateButton()
    {
        if (modinfo.IsAM2R11Loaded &&                                                  // AM2R_11 zip must be provided
            (!windowsCheckBox.Checked.Value || modinfo.IsWindowsModLoaded) &&               // either Windows is disabled OR windows is provided
            (!apkCheckBox.Checked.Value || modinfo.IsApkModLoaded) &&                       // either APK is disabled OR APK is provided
            (!linuxCheckBox.Checked.Value || modinfo.IsLinuxModLoaded) &&                   // either Linux is disabled OR linux is provided
            (!macCheckBox.Checked.Value || modinfo.IsMacModLoaded) &&                       // either Mac is disabled OR mac is provided
            (modinfo.IsWindowsModLoaded || modinfo.IsLinuxModLoaded || modinfo.IsMacModLoaded) && // one desktop OS has to be selected
            (!customSaveCheckBox.Checked.Value || customSaveTextBox.Text != ""))         // either custom saves are disabled OR custom save is provided
            createButton.Enabled = true;
        else
            createButton.Enabled = false;
    }
    
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