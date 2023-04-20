using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using AtomicLib.XML;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using AtomicLib;
using Atomic.Language;
using System.Globalization;
using System.Linq;

namespace Atomic;

public partial class ModPacker : Form
{
    public ModPacker()
    {
        // Fill in lookup tables:
        #region Lookup Table filling
        labelLookupTable = new Dictionary<ProfileOperatingSystems, Label>()
        {
            { ProfileOperatingSystems.Windows, windowsLabel },
            { ProfileOperatingSystems.Linux, linuxLabel },
            { ProfileOperatingSystems.Mac, macLabel },
            { ProfileOperatingSystems.Android, apkLabel },
        };
        buttonLookupTable = new Dictionary<ProfileOperatingSystems, Button>()
        {
            { ProfileOperatingSystems.Windows, windowsButton },
            { ProfileOperatingSystems.Linux, linuxButton },
            { ProfileOperatingSystems.Mac, macButton },
            { ProfileOperatingSystems.Android, apkButton },
        };
        checkboxLookupTable = new Dictionary<ProfileOperatingSystems, CheckBox>()
        {
            { ProfileOperatingSystems.Windows, windowsCheckBox },
            { ProfileOperatingSystems.Linux, linuxCheckBox },
            { ProfileOperatingSystems.Mac, macCheckBox },
            { ProfileOperatingSystems.Android, apkCheckBox },
        };
        modPathLookupTable = new Dictionary<ProfileOperatingSystems, FieldInfo>()
        {
            { ProfileOperatingSystems.Windows, modInfo.GetType().GetField(nameof(modInfo.WindowsModPath)) },
            { ProfileOperatingSystems.Linux, modInfo.GetType().GetField(nameof(modInfo.LinuxModPath)) },
            { ProfileOperatingSystems.Mac, modInfo.GetType().GetField(nameof(modInfo.MacModPath)) },
            { ProfileOperatingSystems.Android, modInfo.GetType().GetField(nameof(modInfo.ApkModPath)) },
        };
        isModLoadedLookupTable = new Dictionary<ProfileOperatingSystems, PropertyInfo>()
        {
            { ProfileOperatingSystems.Windows, modInfo.GetType().GetProperty(nameof(modInfo.IsWindowsModLoaded)) },
            { ProfileOperatingSystems.Linux, modInfo.GetType().GetProperty(nameof(modInfo.IsLinuxModLoaded)) },
            { ProfileOperatingSystems.Mac, modInfo.GetType().GetProperty(nameof(modInfo.IsMacModLoaded)) },
            { ProfileOperatingSystems.Android, modInfo.GetType().GetProperty(nameof(modInfo.IsApkModLoaded)) },
        };
        #endregion
        
        currentConfig = Config.LoadAndReturnConfig();
        
        if (!currentConfig.Language.Equals("SystemLanguage"))
        { 
           CultureInfo language = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.NativeName.ToLower().Contains(currentConfig.Language.ToLower()));
           if (language is null) currentConfig.Language = "SystemLanguage";
           else Thread.CurrentThread.CurrentUICulture = language;
        }
        
        #region Control Text Assignments
        nameLabel.Text = Text.ModName;
        authorLabel.Text = Text.Author;
        versionLabel.Text = Text.Version;
        modNotesLabel.Text = Text.ModNotes;

        customSaveCheckBox.Text = Text.UsesCustomSaves;
        customSaveButton.Text = Text.SelectFolder;
        musicCheckBox.Text = Text.UsesCustomMusic;

        yycCheckBox.Text = Text.UsesYYC;

        windowsCheckBox.Text = String.Format(Text.SupportsOS, Text.Windows);
        windowsButton.Text = String.Format(Text.LoadOSZip, Text.Windows);
        windowsLabel.Text = String.Format(Text.OSGameLoaded, Text.Windows);

        apkCheckBox.Text = Text.BundleAndroid;
        apkButton.Text = Text.LoadAndroidAPK;
        apkLabel.Text = Text.AndroidLoaded;

        linuxCheckBox.Text = String.Format(Text.SupportsOS, Text.Linux);
        linuxButton.Text = String.Format(Text.LoadOSZip, Text.Linux);
        linuxLabel.Text = String.Format(Text.OSGameLoaded, Text.Linux);

        macCheckBox.Text = String.Format(Text.SupportsOS, Text.Mac);
        macButton.Text = String.Format(Text.LoadOSZip, Text.Mac);
        macLabel.Text = String.Format(Text.OSGameLoaded, Text.Mac);

        originalZipButton.Text = Text.LoadAM2R11;
        originalZipLabel.Text = Text.AM2R11Loaded;
        createButton.Text = Text.CreateModPackage;
        createLabel.Text = Text.ModPackageCreated;
        #endregion
        
        Title = "Atomic v" + Version;
        // TODO: "eto bug", this crashes because apparently "compressed symbols aren't supported".
        //Icon = new Icon(new MemoryStream(Resources.icon64)); 
        Icon = new Icon(1f, new Bitmap(Resources.icon64));
        
        MinimumSize = new Size(300, 200);

        var mainContent = new DynamicLayout() { Spacing = new Size(15, 15) };
        var leftSide = new DynamicLayout() { Padding = 10, Spacing = new Size(5, 5) };
        var rightSide = new DynamicLayout() { Padding = 10, Spacing = new Size(5, 5), };
        var rightSideScrollable = new Scrollable() { Border = BorderType.None};

        // Left side
        var modInfoPanel = new DynamicLayout() { Spacing = new Size(5, 5) };
        modInfoPanel.AddRow(nameLabel, nameTextBox);
        modInfoPanel.AddRow(authorLabel, authorTextBox);
        modInfoPanel.AddRow(versionLabel, versionTextBox);
        modInfoPanel.AddRow(modNotesLabel);

        var modNotesPanel = new DynamicLayout();
        modNotesPanel.AddRow(modNotesTextBox);

        // Combine together
        leftSide.AddRow(modInfoPanel);
        leftSide.AddRow(modNotesPanel);

        // Right Side
        var savePanel = new DynamicLayout();
        savePanel.AddRow(customSaveCheckBox, null, customSaveButton);

        var saveTextPanel = new DynamicLayout();
        saveTextPanel.AddRow(customSaveTextBox);

        var miscOptionsPanel = new DynamicLayout() { Spacing = new Size(5, 5) };
        miscOptionsPanel.AddRow(musicCheckBox);
        miscOptionsPanel.AddRow(yycCheckBox);
        miscOptionsPanel.AddRow(windowsCheckBox);
        miscOptionsPanel.AddRow(windowsButton, windowsLabel);
        miscOptionsPanel.AddRow(linuxCheckBox);
        miscOptionsPanel.AddRow(linuxButton, linuxLabel);
        miscOptionsPanel.AddRow(macCheckBox);
        miscOptionsPanel.AddRow(macButton, macLabel);
        miscOptionsPanel.AddRow(apkCheckBox);
        miscOptionsPanel.AddRow(apkButton, apkLabel);

        var loadZipsPanel = new DynamicLayout() { Spacing = new Size(5, 5) };
        loadZipsPanel.AddRow(originalZipButton, originalZipLabel);

        var resultPanel = new DynamicLayout() { Spacing = new Size(5, 5) };
        resultPanel.AddRow(createButton);
        resultPanel.AddRow(createLabel);

        // Combine together
        rightSide.AddRow(savePanel);
        rightSide.AddRow(saveTextPanel);
        rightSide.AddRow(miscOptionsPanel);
        rightSide.AddRow(new Label() { Text = "I am a spacer!", TextColor = this.BackgroundColor });
        rightSide.AddRow(loadZipsPanel);
        rightSide.AddRow(resultPanel);
        rightSideScrollable.Content = rightSide;
        
        // Combine all into main panel and assign
        Splitter splitter = new Splitter() {};
        splitter.Panel1 = leftSide;
        splitter.Panel2 = rightSideScrollable;
        splitter.Panel1MinimumSize = 180;
        splitter.Panel2MinimumSize = 250;
        splitter.Orientation = Orientation.Horizontal;
        // HACK: (Client)Size is bugged on GTK before screen is drawn, so I'm having the width hardcoded
        splitter.Position =  550 / 2;
        
        mainContent.Add(splitter);
        Content = mainContent;

        // Assign events
        originalZipButton.Click += OriginalZipButton_Click;
        createButton.Click += CreateButton_Click;
        windowsCheckBox.CheckedChanged += (_, _) => OSCheckboxChanged(ProfileOperatingSystems.Windows);
        windowsButton.Click += (_, _) => OSButtonClicked(ProfileOperatingSystems.Windows);
        apkCheckBox.CheckedChanged += (_, _) => OSCheckboxChanged(ProfileOperatingSystems.Android);
        apkButton.Click += (_, _) => OSButtonClicked(ProfileOperatingSystems.Android);
        linuxCheckBox.CheckedChanged += (_, _) => OSCheckboxChanged(ProfileOperatingSystems.Linux);
        linuxButton.Click += (_, _) => OSButtonClicked(ProfileOperatingSystems.Linux);
        macCheckBox.CheckedChanged += MacCheckBox_CheckedChanged;
        macButton.Click += (_, _) => OSButtonClicked(ProfileOperatingSystems.Mac);
        customSaveCheckBox.CheckedChanged += CustomSaveCheckBoxChecked_Changed;
        customSaveButton.Click += CustomSaveDataButton_Click;
        yycCheckBox.CheckedChanged += YYCCheckBox_CheckedChanged;
        this.Closing += ModPacker_Closing;
        
        if (currentConfig.FillInContents && currentConfig.Fields != null)
        {
            nameTextBox.Text =  currentConfig.Fields.ModName;
            authorTextBox.Text =  currentConfig.Fields.Author;
            versionTextBox.Text =  currentConfig.Fields.Version;
            modNotesTextBox.Text =  currentConfig.Fields.Notes;
            customSaveCheckBox.Checked =  currentConfig.Fields.UsesCustomSave;
            customSaveTextBox.Text = currentConfig.Fields.CustomSaveDir;
            musicCheckBox.Checked =  currentConfig.Fields.UsesCustomMusic;
            yycCheckBox.Checked =  currentConfig.Fields.UsesYYC;
            windowsCheckBox.Checked =  currentConfig.Fields.SupportsWindows;
            linuxCheckBox.Checked =  currentConfig.Fields.SupportsLinux;
            macCheckBox.Checked =  currentConfig.Fields.SupportsMac;
            apkCheckBox.Checked =  currentConfig.Fields.SupportsAndroid;
        }
        
        // Menu items
        var settings = new Command() { MenuText = Text.SettingsMenu, Shortcut = Application.Instance.CommonModifier | Application.Instance.AlternateModifier | Keys.P};
        settings.Executed += (sender, args) =>
        {
            var settings = new SettingsForm(currentConfig);
            settings.ShowModal();
        };
        var quit = new Command() { MenuText = Text.QuitMenu, Shortcut = Application.Instance.CommonModifier | Keys.Q};
        quit.Executed += (sender, args) => Application.Instance.Quit();

        var file = new SubMenuItem() { Text = Text.FileMenu, Items = { settings, quit } };
        Menu = new MenuBar() {Items = { file }};
    }

    private Config currentConfig;
    
    #region Design Elements
    private Label nameLabel = new Label();
    private TextBox nameTextBox = new TextBox();
    private Label authorLabel = new Label();
    private TextBox authorTextBox = new TextBox();
    private Label versionLabel = new Label();
    private TextBox versionTextBox = new TextBox();
    private Label modNotesLabel = new Label();
    private TextArea modNotesTextBox = new TextArea();

    private CheckBox customSaveCheckBox = new CheckBox();
    private Button customSaveButton = new Button() { Enabled = false };
    // TODO: remove read only and make it correctly respond to user input
    private TextBox customSaveTextBox = new TextBox() { ReadOnly = true };
    private CheckBox musicCheckBox = new CheckBox();
    
    private CheckBox yycCheckBox = new CheckBox();

    private CheckBox windowsCheckBox = new CheckBox();
    private Button windowsButton = new Button() { Enabled = false };
    private Label windowsLabel = new Label() { Visible = false };

    private CheckBox apkCheckBox = new CheckBox();
    private Button apkButton = new Button() { Enabled = false };
    private Label apkLabel = new Label() { Visible = false };

    private CheckBox linuxCheckBox = new CheckBox();
    private Button linuxButton = new Button() { Enabled = false };
    private Label linuxLabel = new Label() { Visible = false };

    private CheckBox macCheckBox = new CheckBox();
    private Button macButton = new Button() { Enabled = false };
    private Label macLabel = new Label() { Visible = false };

    private Button originalZipButton = new Button();
    private Label originalZipLabel = new Label() { Visible = false };
    private Button createButton = new Button() { Enabled = false };
    private Label createLabel = new Label() { Visible = false };
    #endregion
}