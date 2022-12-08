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
    private bool isOriginalLoaded, isModLoaded, isApkLoaded, isLinuxLoaded;
    // TODO: do not get current directory, wont end well
    private readonly string localPath = Directory.GetCurrentDirectory();
    private string originalPath, modPath, apkPath, linuxPath;
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
        // TODO: make sure that the validation works on other platforms too
        var winSaveRegex = new Regex(@"C:\\Users\\.*\\AppData\\Local\\"); //this is to ensure, that the save directory is valid. so far, this is only important for windows
        var linSaveRegex = new Regex(@"$/\.config"); // Linux users can set their home directory wherever they want to, for each individual application 
        //TODO: var macSaveRegex

        var dialog = new SelectFolderDialog();
        // TODO: .config on linux
        dialog.Directory = Environment.GetEnvironmentVariable("LocalAppData");
        while (!wasSuccessful)
        {
            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                Match match;
                match = winSaveRegex.Match(dialog.Directory);
                if (match.Success == false)
                {
                    // TODO: different error messages
                    MessageBox.Show("Invalid Save Directory! Please choose one in %LocalAppData%");
                }
                else
                {
                    //TODO: needs to be OS specific
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
        
        (bool successful, string errorcode) = Core.CreateModPack(profile, modPath,originalPath, apkPath, output);
        if (!successful)
        {
            MessageBox.Show(errorcode, "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            AbortPatch();
        }

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
            (successful, errorcode) = Core.CreateModPack(profile, linuxPath, originalPath, apkPath, output);
            if (!successful)
            {
                MessageBox.Show(errorcode, "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                AbortPatch();
            }
        }
        //TODO: mac

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