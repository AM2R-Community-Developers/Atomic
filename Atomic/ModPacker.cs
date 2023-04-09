using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Atomic.Language;
using AtomicLib;
using Eto.Forms;

namespace Atomic;

public partial class ModPacker : Form
{
    private const string Version = Core.Version;
    private readonly FileFilter apkFileFilter = new FileFilter(Text.APKFileFilter, ".apk");

    private readonly FileFilter zipFileFilter = new FileFilter(Text.ZipArchivesFileFilter, ".zip");
    private readonly Dictionary<ProfileOperatingSystems, Button> buttonLookupTable;
    private readonly Dictionary<ProfileOperatingSystems, CheckBox> checkboxLookupTable;
    private readonly Dictionary<ProfileOperatingSystems, PropertyInfo> isModLoadedLookupTable;

    // Lookup dictionaries, filled in Constructor
    private readonly Dictionary<ProfileOperatingSystems, Label> labelLookupTable;

    private readonly ModCreationInfo modInfo = new ModCreationInfo();
    private readonly Dictionary<ProfileOperatingSystems, FieldInfo> modPathLookupTable;
    
    #region Eto events

    private void CustomSaveCheckBoxChecked_Changed(object sender, EventArgs e)
    {
        customSaveButton.Enabled = customSaveCheckBox.Checked.Value;
        customSaveTextBox.Enabled = customSaveCheckBox.Checked.Value;
        UpdateCreateButton();
    }

    private void CustomSaveDataButton_Click(object sender, EventArgs e)
    {
        // TODO: get rid of the replace after doing the todo right below this. Currently necessary, as otherwise its trying to i.e. do a regex of "C:\Users\foo", and \U is not a valid regex escape sequence.
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "\\\\"); // This is \ -> \\
        bool wasSuccessful = false;
        // TODO: instead of creating 3 different regexes, only create one regex and fill it out depending on current OS. Just leech off of the os check a bit further down for that.
        Regex winSaveRegex = new Regex(@"C:\\Users\\.*\\AppData\\Local\\"); //this is to ensure, that the save directory is valid.
        Regex linSaveRegex = new Regex($@"{home}/\.config/"); // GMS hardcodes save into ~/.config on linux 
        Regex macSaveRegex = new Regex($@"{home}/Library/Application Support/");

        SelectFolderDialog dialog = new SelectFolderDialog();
        string initialDir = "";
        if (OS.IsWindows)
            initialDir = Environment.GetEnvironmentVariable("LocalAppData");
        else if (OS.IsLinux)
            initialDir = home + "/.config";
        else if (OS.IsMac)
            initialDir = $@"{home}/Library/Application Support/";

        dialog.Directory = initialDir;
        // TODO: clean this while loop a little
        string saveFolderPath = null;
        while (!wasSuccessful)
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
                {
                    MessageBox.Show(this, Text.InvalidSaveDirectory);
                }
                else
                {
                    wasSuccessful = true;
                    saveFolderPath = dialog.Directory.Replace(match.Value, "%localappdata%/");
                    // If we don't do this, custom save locations are going to fail on Linux
                    if (OS.IsWindows)
                        saveFolderPath = saveFolderPath.Replace("\\", "/");

                    // On Mac, we need to adjust the path
                    if (OS.IsMac)
                        saveFolderPath = saveFolderPath.Replace("com.yoyogames.am2r", "AM2R");

                    // if someone has a custom save path inside of am2r and creates these within game maker, they will always be lower case
                    // we need to adjust them here to lowercase as well, as otherwise launcher gets problems on nix systems
                    const string vanillaPrefix = "%localappdata%/AM2R/";
                    if (saveFolderPath.Contains(vanillaPrefix))
                        saveFolderPath = vanillaPrefix + saveFolderPath.Substring(vanillaPrefix.Length).ToLower();
                }
            }
            else
            {
                wasSuccessful = true;
            }

        customSaveTextBox.Text = saveFolderPath;
        UpdateCreateButton();
    }

    private void YYCCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        // Don't do anything if it's been disabled
        if (!yycCheckBox.Checked.Value)
            return;
        // Disable mac stuff, as its incompatible with yyc
        macCheckBox.Checked = false;
        macLabel.Visible = false;
        macButton.Enabled = false;
        modInfo.MacModPath = null;
    }

    private void MacCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        if (!yycCheckBox.Checked.Value)
        {
            OSCheckboxChanged(ProfileOperatingSystems.Mac);
        }
        else if (macCheckBox.Checked.Value)
        {
            MessageBox.Show(this, Text.YYCMacUnsupported, Text.Warning, MessageBoxButtons.OK, MessageBoxType.Warning);
            macCheckBox.Checked = false;
        }
    }

    private void OriginalZipButton_Click(object sender, EventArgs e)
    {
        // Open window to select AM2R 1.1
        modInfo.AM2R11Path = SelectFile(Text.SelectAM2R11FileDialog, zipFileFilter);
        originalZipLabel.Visible = modInfo.IsAM2R11Loaded;
        UpdateCreateButton();
    }

    private void CreateButton_Click(object sender, EventArgs e)
    {
        // TODO: use string.isnullorwhitespace
        if (nameTextBox.Text == "" || authorTextBox.Text == "" || versionTextBox.Text == "")
        {
            MessageBox.Show(this, Text.FieldsMissing, Text.Error, MessageBoxButtons.OK, MessageBoxType.Error);
            return;
        }

        // Trim the mod name textbox so that it has no leading/ending whitespace.
        // Also check for invalid file names
        nameTextBox.Text = nameTextBox.Text.Trim();

        if (Path.GetInvalidFileNameChars().Any(nameTextBox.Text.Contains))
        {
            MessageBox.Show(this, Text.NameInvalidCharacters + "\n" + String.Join("\n", Path.GetInvalidFileNameChars()));
            return;
        }

        // Verify 1.1
        IsZipAM2R11ReturnCodes result11 = Core.CheckIfZipIsAM2R11(modInfo.AM2R11Path);
        if (result11 != IsZipAM2R11ReturnCodes.Successful)
        {
            MessageBox.Show(this, Text.AM2R11Invalid + " " + result11);
            AbortPatch();
            return;
        }

        createLabel.Visible = true;
        createLabel.Text = Text.PackagingMods;

        bool PromptAndSaveOSMod(ProfileOperatingSystems os)
        {
            string output;

            using (SaveFileDialog saveFile = new SaveFileDialog { Title = Text.SaveOSModProfile.Replace("$1", GetLocalizedStringOfOS(os)), Filters = { zipFileFilter } })
            {
                if (saveFile.ShowDialog(this) == DialogResult.Ok)
                {
                    output = saveFile.FileName;
                }
                else
                {
                    createLabel.Text = Text.ModPackagingAborted;
                    return false;
                }
            }

            // Some filepickers don't automatically set the file extension
            if (!output.ToLower().EndsWith(".zip"))
                output += ".zip";
            LoadProfileParameters(os);
            try
            {
                Core.CreateModPack(modInfo, output);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.ToString(), Text.Error, MessageBoxButtons.OK, MessageBoxType.Error);
                AbortPatch();
                return false;
            }

            return true;
        }

        bool CheckForProfileXML(ZipArchive zipFile)
        {
            if (zipFile.Entries.All(f => f.Name != "profile.xml"))
                return true;
            DialogResult result = MessageBox.Show(this, Text.ProfileXMLFound, Text.Warning, MessageBoxButtons.YesNo, MessageBoxType.Warning);
            if (result == DialogResult.Yes)
                return true;
            AbortPatch();
            return false;
        }

        // TODO: look if its not possible to generalize these
        if (windowsCheckBox.Checked.Value)
        {
            ZipArchive windowsZip = ZipFile.Open(modInfo.WindowsModPath, ZipArchiveMode.Read);
            if (windowsZip.Entries.All(f => f.FullName != "AM2R.exe"))
            {
                // TODO: make method for these $1 replacements
                DialogResult result = MessageBox.Show(this, Text.ModdedGameNotFound.Replace("$1", Text.Windows), Text.Warning, MessageBoxButtons.YesNo, MessageBoxType.Warning);
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
            ZipArchive linuxZip = ZipFile.Open(modInfo.LinuxModPath, ZipArchiveMode.Read);
            if (linuxZip.Entries.All(f => f.FullName != "AM2R") && linuxZip.Entries.All(f => f.FullName != "runner"))
            {
                DialogResult result = MessageBox.Show(this, Text.ModdedGameNotFound.Replace("$1", Text.Linux), Text.Warning, MessageBoxButtons.YesNo, MessageBoxType.Warning);
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
            ZipArchive macZip = ZipFile.Open(modInfo.MacModPath, ZipArchiveMode.Read);
            if (macZip.Entries.All(f => f.FullName != "AM2R.app/Contents/MacOS/Mac_Runner"))
            {
                DialogResult result = MessageBox.Show(this, Text.ModdedGameNotFound.Replace("$1", Text.Mac), Text.Warning, MessageBoxButtons.YesNo, MessageBoxType.Warning);
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

    private void ModPacker_Closing(object sender, EventArgs e)
    {
        if (currentConfig.FillInContents)
        {
            currentConfig.Fields.ModName = nameTextBox.Text;
            currentConfig.Fields.Author = authorTextBox.Text;
            currentConfig.Fields.Version = versionTextBox.Text;
            currentConfig.Fields.Notes = modNotesTextBox.Text;
            currentConfig.Fields.UsesCustomSave = customSaveCheckBox.Checked.Value;
            currentConfig.Fields.CustomSaveDir = customSaveTextBox.Text;
            currentConfig.Fields.UsesCustomMusic = musicCheckBox.Checked.Value;
            currentConfig.Fields.UsesYYC = yycCheckBox.Checked.Value;
            currentConfig.Fields.SupportsWindows = windowsCheckBox.Checked.Value;
            currentConfig.Fields.SupportsLinux = linuxCheckBox.Checked.Value;
            currentConfig.Fields.SupportsMac = macCheckBox.Checked.Value;
            currentConfig.Fields.SupportsAndroid = apkCheckBox.Checked.Value;
        }

        Config.SaveConfig(currentConfig);
    }
    
    #endregion
    
    private static string GetLocalizedStringOfOS(ProfileOperatingSystems os)
    {
        return os switch
        {
            ProfileOperatingSystems.Windows => Text.Windows,
            ProfileOperatingSystems.Linux => Text.Linux,
            ProfileOperatingSystems.Mac => Text.Mac,
            ProfileOperatingSystems.Android => Text.Android,
            _ => "Unknown"
        };
    }
    
     private void LoadProfileParameters(ProfileOperatingSystems operatingSystem)
    {
        // TODO: give the control events where they assign the values directly?
        modInfo.Profile.Name = nameTextBox.Text;
        modInfo.Profile.Author = authorTextBox.Text;
        modInfo.Profile.Version = versionTextBox.Text;
        modInfo.Profile.UsesCustomMusic = musicCheckBox.Checked.Value;
        modInfo.Profile.UsesYYC = yycCheckBox.Checked.Value;
        modInfo.Profile.SupportsAndroid = apkCheckBox.Checked.Value;
        modInfo.Profile.ProfileNotes = modNotesTextBox.Text;
        modInfo.Profile.OperatingSystem = operatingSystem.ToString();
        if (customSaveCheckBox.Checked.Value && !String.IsNullOrWhiteSpace(customSaveTextBox.Text))
            modInfo.Profile.SaveLocation = customSaveTextBox.Text;
        else
            modInfo.Profile.SaveLocation = "%localappdata%/AM2R";
        if (operatingSystem == ProfileOperatingSystems.Linux)
        {
            modInfo.Profile.SaveLocation = modInfo.Profile.SaveLocation.Replace("%localappdata%", "~/.config");
        }
        else if (operatingSystem == ProfileOperatingSystems.Mac)
        {
            if (modInfo.Profile.SaveLocation.Contains("%localappdata%/AM2R"))
                modInfo.Profile.SaveLocation = modInfo.Profile.SaveLocation.Replace("%localappdata%/AM2R", "~/Library/Application Support/com.yoyogames.am2r");
            else
                modInfo.Profile.SaveLocation = "~/Library/Application Support/com.yoyogames." + new DirectoryInfo(modInfo.Profile.SaveLocation).Name.ToLower();
        }
    }

    private void AbortPatch()
    {
        // Set labels
        createLabel.Text = Text.ModPackagingAborted;

        // Remove temp directory
        if (Directory.Exists(Path.GetTempPath() + "/Atomic"))
            Directory.Delete(Path.GetTempPath() + "/Atomic", true);
    }

    private void UpdateCreateButton()
    {
        if (modInfo.IsAM2R11Loaded && // AM2R_11 zip must be provided
            (!windowsCheckBox.Checked.Value || modInfo.IsWindowsModLoaded) && // either Windows is disabled OR windows is provided
            (!apkCheckBox.Checked.Value || modInfo.IsApkModLoaded) && // either APK is disabled OR APK is provided
            (!linuxCheckBox.Checked.Value || modInfo.IsLinuxModLoaded) && // either Linux is disabled OR linux is provided
            (!macCheckBox.Checked.Value || modInfo.IsMacModLoaded) && // either Mac is disabled OR mac is provided
            (modInfo.IsWindowsModLoaded || modInfo.IsLinuxModLoaded || modInfo.IsMacModLoaded) && // one desktop OS has to be selected
            (!customSaveCheckBox.Checked.Value || customSaveTextBox.Text != "")) // either custom saves are disabled OR custom save is provided
            createButton.Enabled = true;
        else
            createButton.Enabled = false;
    }

    private string SelectFile(string title, FileFilter filter)
    {
        using OpenFileDialog fileFinder = new OpenFileDialog { Filters = { filter } };
        fileFinder.Title = title;
        fileFinder.CurrentFilter = fileFinder.Filters.First();
        fileFinder.CheckFileExists = true;

        if (fileFinder.ShowDialog(this) != DialogResult.Ok)
            return null;

        string location = fileFinder.FileName;
        return location;
    }

    private void OSCheckboxChanged(ProfileOperatingSystems os)
    {
        CheckBox osCheckbox = checkboxLookupTable[os];
        Button osButton = buttonLookupTable[os];
        Label osLabel = labelLookupTable[os];
        FieldInfo osModPath = modPathLookupTable[os];
        // If it was disabled, clean the appropriate attributes
        osButton.Enabled = osCheckbox.Checked.Value;
        if (!osCheckbox.Checked.Value)
        {
            osLabel.Visible = false;
            osModPath.SetValue(modInfo, null);
        }

        UpdateCreateButton();
    }

    private void OSButtonClicked(ProfileOperatingSystems os)
    {
        string pickerMessage = Text.SelectModdedFile.Replace("$1", GetLocalizedStringOfOS(os)).Replace("$2", os == ProfileOperatingSystems.Android ? Text.APK : Text.Zip);
        Label osLabel = labelLookupTable[os];
        FieldInfo osModPath = modPathLookupTable[os];
        PropertyInfo isOsModLoaded = isModLoadedLookupTable[os];
        // Open window to select modded file
        string selectedFile = SelectFile(pickerMessage, os == ProfileOperatingSystems.Android ? apkFileFilter : zipFileFilter);
        osModPath.SetValue(modInfo, selectedFile);
        osLabel.Visible = (bool)isOsModLoaded.GetValue(modInfo);
        UpdateCreateButton();
    }
}