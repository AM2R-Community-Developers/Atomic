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
    private bool isOriginalLoaded, isWindowsLoaded, isApkLoaded, isLinuxLoaded, isMacLoaded;
    private string originalPath, windowsPath, apkPath, linuxPath, macPath;
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
    
    private void CustomSaveDataButton_Click(object sender, EventArgs e)
    {
        bool wasSuccessful = false;
        var winSaveRegex = new Regex(@"C:\\Users\\.*\\AppData\\Local\\");                           //this is to ensure, that the save directory is valid. so far, this is only important for windows
        var linSaveRegex = new Regex($@"{Environment.GetEnvironmentVariable("HOME")}/\.config/.*"); // GMS hardcodes save into ~/.config on linux 
        
        //TODO: var macSaveRegex = ????

        var dialog = new SelectFolderDialog();
        string initialDir = "";
        if (OS.IsWindows)
            initialDir = Environment.GetEnvironmentVariable("LocalAppData");
        else if (OS.IsLinux)
            initialDir = Environment.GetEnvironmentVariable("HOME") + "/.config";
        else if (OS.IsMac)
            initialDir = ""; //TODO!
        
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
                    match = winSaveRegex.Match(dialog.Directory); //TODO!
                
                if (match.Success == false)
                    MessageBox.Show("Invalid Save Directory! Please consult the GameMaker: Studio 1.4 documentation for valid save directories!");
                else
                {
                    //TODO: figure out mac
                    wasSuccessful = true;
                    saveFilePath = dialog.Directory.Replace(match.Value, "%localappdata%/");
                    if (OS.IsWindows) 
                    { 
                        saveFilePath = saveFilePath.Replace("\\", "/"); // if we don't do this, custom save locations are going to fail on Linux
                    }
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
    
    private void YYCCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        // Don't do anything if it's been disabled
        if (!yycCheckBox.Checked.Value)
            return;
        macCheckBox.Checked = false;
        macLabel.Visible = false;
        macButton.Enabled = false;
        macPath = "";
    }

    private void WindowsCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        windowsButton.Enabled = windowsCheckBox.Checked.Value;
        // If it was disabled, clean the appropriate attributes
        if (!windowsCheckBox.Checked.Value)
        {
            isWindowsLoaded = false;
            windowsLabel.Visible = false;
            windowsPath = "";
        }
        UpdateCreateButton();
    }
    
    private void WindowsButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded Linux .zip
        (isWindowsLoaded, windowsPath) = SelectFile("Please select your custom Windows AM2R .zip", zipFileFilter);
        windowsLabel.Visible = isWindowsLoaded;
        UpdateCreateButton();
    }
    
    private void ApkCheckBoxCheckedChanged(object sender, EventArgs e)
    {
        apkButton.Enabled = apkCheckBox.Checked.Value;
        // If it was disabled, clean the appropriate attributes
        if (!apkCheckBox.Checked.Value)
        {
            isApkLoaded = false;
            apkLabel.Visible = false;
            apkPath = "";
        }
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
        // If it was disabled, clean the appropriate attributes
        if (!linuxCheckBox.Checked.Value)
        {
            isLinuxLoaded = false;
            linuxLabel.Visible = false;
            linuxPath = "";
        }
        UpdateCreateButton();
    }

    private void LinuxButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded Linux .zip
        (isLinuxLoaded, linuxPath) = SelectFile("Please select your custom Linux AM2R .zip", zipFileFilter);
        linuxLabel.Visible = isLinuxLoaded;
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
                isMacLoaded = false;
                macLabel.Visible = false;
                macPath = "";
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
        (isMacLoaded, macPath) = SelectFile("Please select your custom Mac AM2R .zip", zipFileFilter);
        macLabel.Visible = isMacLoaded;
        UpdateCreateButton();
    }

    private void OriginalZipButton_Click(object sender, EventArgs e)
    {
        // Open window to select AM2R 1.1
        (isOriginalLoaded, originalPath) = SelectFile("Please select AM2R_11.zip", zipFileFilter);
        originalZipLabel.Visible = isOriginalLoaded;
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
        bool successful;
        string errorCode;

        if (windowsCheckBox.Checked.Value)
        {
            var windowsZip = ZipFile.Open(windowsPath, ZipArchiveMode.Read);
            if (windowsZip.Entries.All(f => f.FullName != "AM2R.exe"))
            {                
                var result = MessageBox.Show("Modded game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return;
                }
            }

            if (windowsZip.Entries.Any(f => f.Name == "profile.xml"))
            {
                var result = MessageBox.Show("profile.xml found. This file is used by the AM2RLauncher to determine profile stats and its inclusion may make the profile uninstallable. Are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return;
                }
            }
            
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
            (successful, errorCode) = Core.CreateModPack(profile, originalPath, windowsPath, apkPath, output);
            if (!successful)
            {
                MessageBox.Show(errorCode, "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                AbortPatch();
            }
        }

        if (linuxCheckBox.Checked.Value)
        {
            var linuxZip = ZipFile.Open(linuxPath, ZipArchiveMode.Read);
            if (linuxZip.Entries.All(f => f.FullName != "AM2R") && linuxZip.Entries.All(f => f.FullName != "runner"))
            { 
                var result = MessageBox.Show("Modded Linux game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return;
                }
            }

            if (linuxZip.Entries.Any(f => f.Name == "profile.xml"))
            {
                var result = MessageBox.Show("profile.xml found. This file is used by the AM2RLauncher to determine profile stats and its inclusion may make the profile uninstallable. Are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);            if (result != DialogResult.Yes)
                {
                    AbortPatch();
                    return;
                }
            }

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
            (successful, errorCode) = Core.CreateModPack(profile, originalPath, linuxPath, apkPath, output);
            if (!successful)
            {
                MessageBox.Show(errorCode, "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                AbortPatch();
            }
        }
        if (macCheckBox.Checked.Value)
        {
            var macZip = ZipFile.Open(macPath, ZipArchiveMode.Read);
            if (macZip.Entries.All(f => f.Name != "AM2R.app/Contents/MacOS/Mac_Runner"))
            {
                var result = MessageBox.Show("Modded Mac game not found, make sure it's not placed in any subfolders.\nCreated profile will likely not be installable, are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                    AbortPatch();
            }

            if (macZip.Entries.Any(f => f.Name == "profile.xml"))
            {
                var result = MessageBox.Show("profile.xml found. This file is used by the AM2RLauncher to determine profile stats and its inclusion may make the profile uninstallable. Are you sure you want to continue?", "WARNING", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                if (result != DialogResult.Yes)
                    AbortPatch();
            }

            using (SaveFileDialog saveFile = new SaveFileDialog { Title = "Save Mac mod profile", Filters = { zipFileFilter } })
            {
                if (saveFile.ShowDialog(this) == DialogResult.Ok)
                    output = saveFile.FileName;
                else
                {
                    createLabel.Text = "Mod packaging aborted!";
                    return;
                }
            }
            Core.CreateModPack(profile, originalPath, macPath, apkPath, output);
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
        // Unload files
        isOriginalLoaded = false;
        isWindowsLoaded = false;
        isApkLoaded = false;
        isLinuxLoaded = false;
        originalPath = "";
        windowsPath = "";
        apkPath = "";
        linuxPath = "";
        saveFilePath = null;

        // Set labels
        createLabel.Text = "Mod packaging aborted!";
        originalZipLabel.Visible = false;
        apkLabel.Visible = false;
        linuxLabel.Visible = false;

        // Remove temp directory
        if (Directory.Exists(Path.GetTempPath() + "/AM2RModPacker"))
            Directory.Delete(Path.GetTempPath() + "/AM2RModPacker", true);
    }
    
    private void UpdateCreateButton()
    {
        if (isOriginalLoaded &&                                                  // AM2R_11 zip must be provided
            (!windowsCheckBox.Checked.Value || isWindowsLoaded) &&               // either Windows is disabled OR windows is provided
            (!apkCheckBox.Checked.Value || isApkLoaded) &&                       // either APK is disabled OR APK is provided
            (!linuxCheckBox.Checked.Value || isLinuxLoaded) &&                   // either Linux is disabled OR linux is provided
            (!macCheckBox.Checked.Value || isMacLoaded) &&                       // either Mac is disabled OR mac is provided
            (isWindowsLoaded || isLinuxLoaded || isMacLoaded) &&                 // one desktop OS has to be selected
            (!customSaveCheckBox.Checked.Value || customSaveTextBox.Text != "")) // either custom saves are disabled OR custom save is provided
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