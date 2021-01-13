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

        private void NameTextBox_TextChanged(object sender, EventArgs e)
        {
            profile.name = NameTextBox.Text;
        }

        private void AuthorTextBox_TextChanged(object sender, EventArgs e)
        {
            profile.author = AuthorTextBox.Text;
        }

        private void MusicCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            profile.usesCustomMusic = MusicCheckBox.Checked;
        }

        private void SaveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (SaveCheckBox.Checked)
            {
                profile.saveLocation = "custom";
            }
            else
            {
                profile.saveLocation = "default";
            }
        }

        private void YYCCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            profile.usesYYC = YYCCheckBox.Checked;
        }

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
            if (Directory.Exists(localPath + "\\temp"))
            {
                Directory.Delete(localPath + "\\temp", true);
            }

            // Create temp work folders
            string tempFolder = Directory.CreateDirectory(localPath + "\\temp").FullName;
            string tempOriginal = Directory.CreateDirectory(tempFolder + "\\original").FullName;
            string tempMod = Directory.CreateDirectory(tempFolder + "\\mod").FullName;
            string tempProfile = Directory.CreateDirectory(tempFolder + "\\profile").FullName;

            // Extract 1.1 and modded AM2R to their own directories in temp work
            ZipFile.ExtractToDirectory(originalLocation, tempOriginal);
            ZipFile.ExtractToDirectory(modLocation, tempMod);

            // Verify 1.1 with an MD5. If it does not match, exit cleanly and provide a warning window.
            if (CalculateMD5(tempOriginal + "\\data.win") != ORIGINAL_MD5)
            {
                // Show error box
                MessageBox.Show("1.1 data.win does not meet MD5 checksum! Mod packaging aborted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Cleanup 
                Directory.Delete(tempFolder, true);

                // Exit function
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
                string[] blacklist = { "data.win", "AM2R.exe", "D3DX9_43.dll" };

                CopyFilesRecursive(dinfo, blacklist, tempProfile + "\\files_to_copy");
            }
            else
            {
                string[] musFiles = new string[Directory.GetFiles(tempOriginal, "*.ogg").Length];

                int i = 0;

                foreach (FileInfo file in new DirectoryInfo(tempOriginal).GetFiles("*.ogg"))
                {
                    musFiles[i] = file.Name;
                    i++;
                }
                // "musAlphaFight.ogg", "musAncientGuardian.ogg", "musArachnus.ogg", "musArea1A.ogg", "musArea1B.ogg", "musArea2A.ogg", "musArea2B.ogg", "musArea3A.ogg", "musArea4A.ogg", "musArea4B.ogg", "musArea5A.ogg", "musArea5B.ogg", "musArea6A.ogg", "musArea7A.ogg", "musArea7B.ogg", "musArea7C.ogg", "musArea7D.ogg", "musArea8.ogg", "musCaveAmbience.ogg", "musCaveAmbienceA4.ogg", "musCredits.ogg", "musEris.ogg", "musFanfare.ogg", "musGammaFight.ogg", "musGenesis.ogg", "musHatchling.ogg"
                string[] dataFiles = { "data.win", "AM2R.exe", "D3DX9_43.dll" };

                string[] blacklist = musFiles.Concat(dataFiles).ToArray();

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

            // Open file explorer window with .zip selected
            Process.Start("explorer.exe", "/select, \"" + output + "\"");
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
            profile.android = AndroidCheckBox.Checked;
            APKButton.Enabled = AndroidCheckBox.Checked;
            UpdateCreateButton();
        }

        #endregion

        private void CopyFilesRecursive(DirectoryInfo source, string[] blacklist, string destination)
        {
            foreach (FileInfo file in source.GetFiles())
            {
                if (!blacklist.Contains(file.Name))
                {
                    file.CopyTo(destination + "\\" + file.Name);
                    /*if (!file.Name.EndsWith(".ogg") || profile.usesCustomMusic)
                    {
                        file.CopyTo(destination + "\\" + file.Name);
                    }*/
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
