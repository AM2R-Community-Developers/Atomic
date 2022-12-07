using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AM2RModPacker.XML;
using Eto.Forms;

namespace AM2RModPacker;

public partial class ModPacker : Form
{
    private static readonly string VERSION = "2.0.3";
    private static readonly string ORIGINAL_MD5 = "f2b84fe5ba64cb64e284be1066ca08ee";
    private bool isOriginalLoaded, isModLoaded, isApkLoaded, isLinuxLoaded;
    private string localPath, originalPath, modPath, apkPath, linuxPath;
    private static readonly string[] DATAFILES_BLACKLIST = { "data.win", "AM2R.exe", "D3DX9_43.dll", "game.unx" };
    private static string saveFilePath = null;
    private ModProfileXML profile;

    private FileFilter zipFileFilter = new FileFilter("zip archives (*.zip)", ".zip");

    #region Eto events

    private void OriginalButton_Click(object sender, EventArgs e)
    {
        // Open window to select AM2R 1.1
        (isOriginalLoaded, originalPath) = SelectFile("Please select AM2R_11.zip", zipFileFilter);

        OriginalLabel.Visible = isOriginalLoaded; 

        UpdateCreateButton();
    }

    private void ModButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded AM2R
        (isModLoaded, modPath) = SelectFile("Please select your custom AM2R .zip", zipFileFilter);

        ModLabel.Visible = isModLoaded;

        UpdateCreateButton();
    }

    private void CreateButton_Click(object sender, EventArgs e)
    {
        if (NameTextBox.Text == "" || AuthorTextBox.Text == "" || versionTextBox.Text == "")
        {
            MessageBox.Show("Text field missing! Mod packaging aborted.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            return;
        }

        if (Path.GetInvalidFileNameChars().Any(NameTextBox.Text.Contains))
        {
            MessageBox.Show("Name contains invalid characters! These characters are not allowed:\n" + string.Join("\n", Path.GetInvalidFileNameChars()));
            return;
        }

        CreateLabel.Visible = true;
        CreateLabel.Text = "Packaging mod(s)... This could take a while!";

        string output;

        using (SaveFileDialog saveFile = new SaveFileDialog { Title = "Save Windows mod profile", Filters = {zipFileFilter} })
        {
            if (saveFile.ShowDialog(this) == DialogResult.Ok)
            {
                output = saveFile.FileName;
            }
            else
            {
                CreateLabel.Text = "Mod packaging aborted!";
                return;
            }
        }

        CreateModPack("Windows", modPath, output);

        if (linuxCheckBox.Checked.Value)
        {
            using (SaveFileDialog saveFile = new SaveFileDialog { Title = "Save Linux mod profile", Filters = {zipFileFilter} })
            {
                if (saveFile.ShowDialog(this) == DialogResult.Ok)
                {
                    output = saveFile.FileName;
                }
                else
                {
                    CreateLabel.Text = "Mod packaging aborted!";
                    return;
                }
            }

            CreateModPack("Linux", linuxPath, output);
        }

        CreateLabel.Text = "Mod package(s) created!";
    }

    private void CreateModPack(string operatingSystem, string input, string output)
    {
        LoadProfileParameters(operatingSystem);

        // Cleanup in case of previous errors
        if (Directory.Exists(Path.GetTempPath() + "\\AM2RModPacker"))
        {
            Directory.Delete(Path.GetTempPath() + "\\AM2RModPacker", true);
        }

        // Create temp work folders
        string tempPath = "",
               tempOriginalPath = "",
               tempModPath = "",
               tempProfilePath = "";

        // We might not have permission to access to the temp directory, so we need to catch the exception.
        try
        {
            tempPath = Directory.CreateDirectory(Path.GetTempPath() + "\\AM2RModPacker").FullName;
            tempOriginalPath = Directory.CreateDirectory(tempPath + "\\original").FullName;
            tempModPath = Directory.CreateDirectory(tempPath + "\\mod").FullName;
            tempProfilePath = Directory.CreateDirectory(tempPath + "\\profile").FullName;
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

        if (Directory.Exists(tempModPath + "\\AM2R"))
            tempModPath += "\\AM2R";

        // Verify 1.1 with an MD5. If it does not match, exit cleanly and provide a warning window.
        try
        {
            string newMD5 = CalculateMD5(tempOriginalPath + "\\data.win");

            if (newMD5 != ORIGINAL_MD5)
            {
                // Show error box
                MessageBox.Show("1.1 data.win does not meet MD5 checksum! Mod packaging aborted.\n1.1 MD5: " + ORIGINAL_MD5 + "\nYour MD5: " + newMD5, "Error", MessageBoxButtons.OK, MessageBoxType.Error);

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
                CreatePatch(tempOriginalPath + "\\data.win", tempModPath + "\\AM2R.exe", tempProfilePath + "\\AM2R.xdelta");
            }
            else
            {
                CreatePatch(tempOriginalPath + "\\data.win", tempModPath + "\\data.win", tempProfilePath + "\\data.xdelta");

                CreatePatch(tempOriginalPath + "\\AM2R.exe", tempModPath + "\\AM2R.exe", tempProfilePath + "\\AM2R.xdelta");
            }
        }
        else if (profile.OperatingSystem == "Linux")
        {
            string runnerName = File.Exists(tempModPath + "\\" + "AM2R") ? "AM2R" : "runner";

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

            CreatePatch(tempOriginalPath + "\\data.win", tempModPath + "\\assets\\game.unx", tempProfilePath + "\\game.xdelta");
            CreatePatch(tempOriginalPath + "\\AM2R.exe", tempModPath + "\\" + runnerName, tempProfilePath + "\\AM2R.xdelta");
        }

        // Create game.droid patch and wrapper if Android is supported
        if (profile.Android)
        {
            string tempAndroid = Directory.CreateDirectory(tempPath + "\\android").FullName;

            // Extract APK 
            // - java -jar apktool.jar d "%~dp0AM2RWrapper_old.apk"

            // Process startInfo
            ProcessStartInfo procStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = tempAndroid,
                Arguments = "/C java -jar \"" + localPath + "\\utilities\\android\\apktool.jar\" d -f -o \"" + tempAndroid + "\" \"" + apkPath + "\"",
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
            CreatePatch(tempOriginalPath + "\\data.win", tempAndroid + "\\assets\\game.droid", tempProfilePath + "\\droid.xdelta");

            // Delete excess files in APK

            // Create whitelist
            string[] whitelist = { "splash.png", "portrait_splash.png" };

            // Get directory
            DirectoryInfo androidAssets = new DirectoryInfo(tempAndroid + "\\assets");

            


            // Delete files
            foreach (FileInfo file in androidAssets.GetFiles())
            {
                if (file.Name.EndsWith(".ini") && file.Name != "modifiers.ini")
                {
                    if (File.Exists(tempProfilePath + "\\AM2R.ini"))
                    {
                        // This shouldn't be a problem... normally...
                        File.Delete(tempProfilePath + "\\AM2R.ini");
                    }
                    File.Copy(file.FullName, tempProfilePath + "\\AM2R.ini");
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
                Arguments = "/C java -jar \"" + localPath + "\\utilities\\android\\apktool.jar\" b -f \"" + tempAndroid + "\" -o \"" + tempProfilePath + "\\AM2RWrapper.apk\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Run process
            using (Process proc = new Process { StartInfo = procStartInfo2 })
            {
                proc.Start();

                proc.WaitForExit();
            }

            string tempAndroidProfilePath = tempProfilePath + "\\android";
            Directory.CreateDirectory(tempAndroidProfilePath);

            File.Move(tempProfilePath + "\\AM2RWrapper.apk", tempAndroidProfilePath + "\\AM2RWrapper.apk");
            if (File.Exists(tempProfilePath + "\\AM2R.ini"))
                File.Move(tempProfilePath + "\\AM2R.ini", tempAndroidProfilePath + "\\AM2R.ini");
        }

        // Copy datafiles (exclude .ogg if custom music is not selected)

        DirectoryInfo dinfo = new DirectoryInfo(tempModPath);
        if (profile.OperatingSystem == "Linux")
            dinfo = new DirectoryInfo(tempModPath + "\\assets");

        Directory.CreateDirectory(tempProfilePath + "\\files_to_copy");

        if (profile.UsesCustomMusic)
        {
            // Copy files, excluding the blacklist
            CopyFilesRecursive(dinfo, DATAFILES_BLACKLIST, tempProfilePath + "\\files_to_copy");
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
            CopyFilesRecursive(dinfo, blacklist, tempProfilePath + "\\files_to_copy");
        }

        // Export profile as XML
        string xmlOutput = Serializer.Serialize<ModProfileXML>(profile);
        File.WriteAllText(tempProfilePath + "\\profile.xml", xmlOutput);

        // Compress temp folder to .zip
        if (File.Exists(output))
        {
            File.Delete(output);
        }

        ZipFile.CreateFromDirectory(tempProfilePath, output);

        // Delete temp folder
        Directory.Delete(tempPath, true);
    }

    private void ApkButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded AM2R APK
        (isApkLoaded, apkPath) = SelectFile("Please select your custom AM2R .apk", "android application packages (*.apk)|*.apk");

        ApkLabel.Visible = isApkLoaded;

        UpdateCreateButton();
    }

    private void AndroidCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        ApkButton.Enabled = AndroidCheckBox.Checked.Value;
        UpdateCreateButton();
    }

    private void SaveCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        winSaveButton.Enabled = SaveCheckBox.Checked.Value;
        saveTextBox.Enabled = SaveCheckBox.Checked.Value;
        UpdateCreateButton();
    }

    #endregion

    private void LoadProfileParameters(string operatingSystem)
    {
        profile.Name = NameTextBox.Text;
        profile.Author = AuthorTextBox.Text;
        profile.Version = versionTextBox.Text;
        profile.UsesCustomMusic = MusicCheckBox.Checked.Value;            
        profile.UsesYYC = YYCCheckBox.Checked.Value;
        profile.Android = AndroidCheckBox.Checked.Value;
        profile.ProfileNotes = modNotesTextBox.Text;
        profile.OperatingSystem = operatingSystem;
        if (SaveCheckBox.Checked.Value && saveTextBox.Text != "")
        {
            profile.SaveLocation = saveTextBox.Text;
        }
        else
        {
            profile.SaveLocation = "%localappdata%/AM2R";
        }
        if (operatingSystem == "Linux")
        {
            profile.SaveLocation = profile.SaveLocation.Replace("%localappdata%", "~/.config");
        }
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
        CreateLabel.Text = "Mod packaging aborted!";
        OriginalLabel.Visible = false;
        ModLabel.Visible = false;
        ApkLabel.Visible = false;
        linuxLabel.Visible = false;

        // Remove temp directory
        if (Directory.Exists(Path.GetTempPath() + "\\AM2RModPacker"))
        {
            Directory.Delete(Path.GetTempPath() + "\\AM2RModPacker", true);
        }
    }

    private void linuxCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        linuxButton.Enabled = linuxCheckBox.Checked.Value;
        UpdateCreateButton();
    }

    private void linuxButton_Click(object sender, EventArgs e)
    {
        // Open window to select modded Linux .zip
        (isLinuxLoaded, linuxPath) = SelectFile("Please select your custom Linux AM2R .zip", zipFileFilter);

        linuxLabel.Visible = isLinuxLoaded;

        UpdateCreateButton();
    }

    private void CustomSaveDataButton_Click(object sender, EventArgs e)
    {
        bool wasSuccessfull = false;
        Regex saveRegex = new Regex(@"C:\\Users\\.*\\AppData\\Local\\");     //this is to ensure, that the save directory is valid. so far, this is only important for windows

        
        SelectFolderDialog dialog = new SelectFolderDialog();
        // currently not implemented in eto
        //dialog.InitialDirectory = Environment.GetEnvironmentVariable("LocalAppData");
        while (!wasSuccessfull)
        {
            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                Match match = saveRegex.Match(dialog.Directory);
                if (match.Success == false)
                    MessageBox.Show("Invalid Save Directory! Please choose one in %LocalAppData%");
                else
                {
                    wasSuccessfull = true;
                    saveFilePath = dialog.Directory.Replace(match.Value, "%localappdata%/");
                    saveFilePath = saveFilePath.Replace("\\", "/");            // if we don't do this, custom save locations are going to fail on Linux
                    // if someone has a custom save path inside of am2r and creates these whithin game maker, they will always be lower case
                    // we need to adjust them here to lowercase as well, as otherwise launcher gets problems on nix systems
                    string vanillaPrefix = "%localappdata%/AM2R/";
                    if (saveFilePath.Contains(vanillaPrefix))
                        saveFilePath = vanillaPrefix + saveFilePath.Substring(vanillaPrefix.Length).ToLower();
                }
            }
            else
            {
                wasSuccessfull = true;
                saveFilePath = null;
            }
        }
        saveTextBox.Text = saveFilePath;
    }

    private void CopyFilesRecursive(DirectoryInfo source, string[] blacklist, string destination)
    {
        foreach (FileInfo file in source.GetFiles())
        {
            if (!blacklist.Contains(file.Name))
            {
                file.CopyTo(destination + "\\" + file.Name);
            }
        }

        foreach (DirectoryInfo dir in source.GetDirectories())
        {
            // Folders need to be lowercase, because GM only reads from lowercase names on *nix systems. Windows is case-insensitive so doesnt matter for them
            string newDir = Directory.CreateDirectory(destination + "\\" + dir.Name.ToLower()).FullName;
            CopyFilesRecursive(dir, blacklist, newDir);
        }
    }

    private void UpdateCreateButton()
    {
        if (isOriginalLoaded && 
            isModLoaded && 
            (!AndroidCheckBox.Checked.Value || isApkLoaded) && 
            (!linuxCheckBox.Checked.Value || isLinuxLoaded) && 
            (!SaveCheckBox.Checked.Value || saveTextBox.Text != ""))
        {
            CreateButton.Enabled = true;
        }
        else
        {
            CreateButton.Enabled = false;
        }
    }

    // Thanks, stackoverflow: https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
    private string CalculateMD5(string filename)
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

    private void CreatePatch(string original, string modified, string output)
    {
        // Specify process start info
        ProcessStartInfo parameters = new ProcessStartInfo
        {
            FileName = localPath + "\\utilities\\xdelta\\xdelta3.exe",
            WorkingDirectory = localPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = "-f -e -s \"" + original + "\" \"" + modified + "\" \"" + output + "\""
        };

        // Launch process and wait for exit. using statement automatically disposes the object for us!
        using (Process proc = new Process { StartInfo = parameters })
        {
            proc.Start();

            proc.WaitForExit();
        }
    }

    private (bool, string) SelectFile(string title, FileFilter filter)
    {
        using (OpenFileDialog fileFinder = new OpenFileDialog() {Filters = { filter }})
        {
            fileFinder.Title = title;
            fileFinder.CurrentFilter = fileFinder.Filters.First();
            fileFinder.CheckFileExists = true;

            if (fileFinder.ShowDialog(this) == DialogResult.Ok)
            {
                string location = fileFinder.FileName;
                return (true, location);

            }
            else return (false, "");
        }
    }
}