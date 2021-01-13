using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;

namespace AM2R_ModPacker
{
    public partial class ModPacker : Form
    {
        private static readonly string ORIGINAL_MD5 = "f2b84fe5ba64cb64e284be1066ca08ee";
        private bool originalLoaded, modLoaded, androidLoaded;
        private string localPath, originalLocation, modLocation, androidLocation;
        private static readonly string[] DATAFILES_BLACKLIST = { "data.win", "AM2R.exe", "D3DX9_43.dll" };
        private ModProfile profile;
        public ModPacker()
        {
            InitializeComponent();
            profile = new ModProfile(1, "", "", false, "default", false, false);
            originalLoaded = false;
            modLoaded = false;
            androidLoaded = false;

            localPath = Directory.GetCurrentDirectory();
            originalLocation = "";
            modLocation = "";
        }

        #region WinForms events

        private void OriginalButton_Click(object sender, EventArgs e)
        {
            // Open window to select AM2R 1.1
            (originalLoaded, originalLocation) = SelectFile("Please select AM2R_11.zip", "zip", "zip files (*.zip)|*.zip");

            OriginalLabel.Visible = originalLoaded; 

            UpdateCreateButton();
        }

        private void ModButton_Click(object sender, EventArgs e)
        {
            // Open window to select modded AM2R
            (modLoaded, modLocation) = SelectFile("Please select your custom AM2R .zip", "zip", "zip files (*.zip)|*.zip");

            ModLabel.Visible = modLoaded;

            UpdateCreateButton();
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            LoadProfileParameters();

            string output;

            if (profile.name == "" || profile.author == "")
            {
                MessageBox.Show("Text field missing! Mod packaging aborted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CreateLabel.Visible = true;
            CreateLabel.Text = "Packaging mod... This could take a while!";

            using (SaveFileDialog saveFile = new SaveFileDialog { InitialDirectory = localPath, Title = "Save mod profile", Filter = "zip files (*.zip)|*.zip", AddExtension = true })
            {
                if(saveFile.ShowDialog() == DialogResult.OK)
                {
                    output = saveFile.FileName;
                }
                else
                {
                    CreateLabel.Text = "Mod packaging aborted!";
                    return;
                }
            }

            // Cleanup in case of previous errors
            if (Directory.Exists(Path.GetTempPath() + "\\AM2RModPacker"))
            {
                Directory.Delete(Path.GetTempPath() + "\\AM2RModPacker", true);
            }

            // Create temp work folders
            // string tempFolder = Directory.CreateDirectory(localPath + "\\temp").FullName;
            string tempFolder = Directory.CreateDirectory(Path.GetTempPath() + "\\AM2RModPacker").FullName;
            string tempOriginal = Directory.CreateDirectory(tempFolder + "\\original").FullName;
            string tempMod = Directory.CreateDirectory(tempFolder + "\\mod").FullName;
            string tempProfile = Directory.CreateDirectory(tempFolder + "\\profile").FullName;

            // Extract 1.1 and modded AM2R to their own directories in temp work
            ZipFile.ExtractToDirectory(originalLocation, tempOriginal);
            ZipFile.ExtractToDirectory(modLocation, tempMod);

            // Verify 1.1 with an MD5. If it does not match, exit cleanly and provide a warning window.
            try
            {
                string newMD5 = CalculateMD5(tempOriginal + "\\data.win");

                if (newMD5 != ORIGINAL_MD5)
                {
                    // Show error box
                    MessageBox.Show("1.1 data.win does not meet MD5 checksum! Mod packaging aborted.\n1.1 MD5: " + ORIGINAL_MD5 + "\nYour MD5: " + newMD5, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    AbortPatch();

                    return;
                }
            }
            catch (FileNotFoundException)
            {
                // Show error message
                MessageBox.Show("data.win not found! Are you sure you selected AM2R 1.1? Mod packaging aborted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                AbortPatch();

                return;
            }

            // Create AM2R.exe and data.win patches
            if (profile.usesYYC)
            {
                CreatePatch(tempOriginal + "\\data.win", tempMod + "\\AM2R.exe", tempProfile + "\\AM2R.xdelta");
            }
            else
            {
                CreatePatch(tempOriginal + "\\data.win", tempMod + "\\data.win", tempProfile + "\\data.xdelta");

                CreatePatch(tempOriginal + "\\AM2R.exe", tempMod + "\\AM2R.exe", tempProfile + "\\AM2R.xdelta");
            }

            // Create game.droid patch and wrapper if Android is supported
            if (profile.android)
            {
                string tempAndroid = Directory.CreateDirectory(tempFolder + "\\android").FullName;

                // Extract APK 
                // - java -jar apktool.jar d "%~dp0AM2RWrapper_old.apk"

                // Process startInfo
                ProcessStartInfo procStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = tempAndroid,
                    Arguments = "/C java -jar \"" + localPath + "\\utilities\\android\\apktool.jar\" d -f -o \"" + tempAndroid + "\" \"" + androidLocation + "\"",
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
                CreatePatch(tempOriginal + "\\data.win", tempAndroid + "\\assets\\game.droid", tempProfile + "\\droid.xdelta");

                // Delete excess files in APK

                // Create whitelist
                string[] whitelist = { "splash.png", "portrait_splash.png"};

                // Get directory
                DirectoryInfo androidAssets = new DirectoryInfo(tempAndroid + "\\assets");

                // Copy *.ini to profile, rename to AM2R.profile


                // Delete files
                foreach (FileInfo file in androidAssets.GetFiles())
                {
                    if (file.Name.EndsWith(".ini") && file.Name != "modifiers.ini")
                    {
                        if (File.Exists(tempProfile + "\\AM2R.ini"))
                        {
                            // This shouldn't be a problem... normally...
                            File.Delete(tempProfile + "\\AM2R.ini");
                        }
                        File.Copy(file.FullName, tempProfile + "\\AM2R.ini");
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
                    Arguments = "/C java -jar \"" + localPath + "\\utilities\\android\\apktool.jar\" b -f \"" + tempAndroid + "\" -o \"" + tempProfile + "\\AM2RWrapper.apk\"",
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

            DirectoryInfo dinfo = new DirectoryInfo(tempMod);

            Directory.CreateDirectory(tempProfile + "\\files_to_copy");

            if (profile.usesCustomMusic)
            {
                // Copy files, excluding the blacklist
                CopyFilesRecursive(dinfo, DATAFILES_BLACKLIST, tempProfile + "\\files_to_copy");
            }
            else
            {
                // Get list of 1.1's music files
                string[] musFiles = Directory.GetFiles(tempOriginal, "*.ogg").Select(file => Path.GetFileName(file)).ToArray();

                // Combine musFiles with the known datafiles for a blacklist
                string[] blacklist = musFiles.Concat(DATAFILES_BLACKLIST).ToArray();

                // Copy files, excluding the blacklist
                CopyFilesRecursive(dinfo, blacklist, tempProfile + "\\files_to_copy");
            }            

            // Export profile as JSON
            string jsonOutput = JsonConvert.SerializeObject(profile);
            File.WriteAllText(tempProfile + "\\modmeta.json", jsonOutput);

            // Compress temp folder to .zip
            if (File.Exists(output))
            {
                File.Delete(output);
            }

            ZipFile.CreateFromDirectory(tempProfile, output);

            // Delete temp folder
            Directory.Delete(tempFolder, true);

            CreateLabel.Text = "Mod package created!";
        }

        private void APKButton_Click(object sender, EventArgs e)
        {
            // Open window to select modded AM2R APK
            (androidLoaded, androidLocation) = SelectFile("Please select your custom AM2R .apk", "apk", "android application packages (*.apk)|*.apk");

            APKLabel.Visible = androidLoaded;

            UpdateCreateButton();
        }

        private void AndroidCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            APKButton.Enabled = AndroidCheckBox.Checked;
            UpdateCreateButton();
        }

        #endregion

        private void LoadProfileParameters()
        {
            profile.name = NameTextBox.Text;
            profile.author = AuthorTextBox.Text;
            profile.usesCustomMusic = MusicCheckBox.Checked;            
            profile.usesYYC = YYCCheckBox.Checked;
            profile.android = AndroidCheckBox.Checked;
            if (SaveCheckBox.Checked)
            {
                profile.saveLocation = "custom";
            }
            else
            {
                profile.saveLocation = "default";
            }
        }

        private void AbortPatch()
        {
            // Unload files
            originalLoaded = false;
            modLoaded = false;
            androidLoaded = false;
            originalLocation = "";
            modLocation = "";
            androidLocation = "";

            // Set labels
            CreateLabel.Text = "Mod packaging aborted!";
            OriginalLabel.Visible = false;
            ModLabel.Visible = false;
            APKLabel.Visible = false;

            // Remove temp directory
            if (Directory.Exists(Path.GetTempPath() + "\\AM2RModPacker"))
            {
                Directory.Delete(Path.GetTempPath() + "\\AM2RModPacker", true);
            }
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
                string newDir = Directory.CreateDirectory(destination + "\\" + dir.Name).FullName;
                CopyFilesRecursive(dir, blacklist, newDir);
            }
        }

        private void UpdateCreateButton()
        {
            if (originalLoaded && modLoaded && (!AndroidCheckBox.Checked || androidLoaded))
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

        private (bool, string) SelectFile(string title, string extension, string filter)
        {
            using (OpenFileDialog fileFinder = new OpenFileDialog())
            {
                fileFinder.InitialDirectory = localPath;
                fileFinder.Title = title;
                fileFinder.DefaultExt = extension;
                fileFinder.Filter = filter;
                fileFinder.CheckFileExists = true;
                fileFinder.CheckPathExists = true;
                fileFinder.Multiselect = false;

                if (fileFinder.ShowDialog() == DialogResult.OK)
                {
                    string location = fileFinder.FileName;
                    return (true, location);

                }
                else return (false, "");
            }
        }
    }
}
