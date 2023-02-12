using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using AtomicLib.XML;
using System.IO;
using System.Reflection;
using AtomicLib;

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
        
        Title = "Atomic v" + version;
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
        macCheckBox.CheckedChanged += macCheckBox_CheckedChanged;
        macButton.Click += (_, _) => OSButtonClicked(ProfileOperatingSystems.Mac);
        customSaveCheckBox.CheckedChanged += CustomSaveCheckBoxChecked_Changed;
        customSaveButton.Click += CustomSaveDataButton_Click;
        yycCheckBox.CheckedChanged += YYCCheckBox_CheckedChanged;
    }

    #region Design Elements
    private Label nameLabel = new Label() { Text = "Mod name:" };
    private TextBox nameTextBox = new TextBox();
    private Label authorLabel = new Label() { Text = "Author:" };
    private TextBox authorTextBox = new TextBox();
    private Label versionLabel = new Label() { Text = "Version:" };
    private TextBox versionTextBox = new TextBox();
    private Label modNotesLabel = new Label() { Text = "Mod notes:" };
    private TextArea modNotesTextBox = new TextArea() { };

    private CheckBox customSaveCheckBox = new CheckBox() { Text = "Uses custom save directory" };
    private Button customSaveButton = new Button() { Text = "Select folder", Enabled = false };
    // TODO: remove read only and make it correctly respond to user input
    private TextBox customSaveTextBox = new TextBox() { ReadOnly = true };
    private CheckBox musicCheckBox = new CheckBox() { Text = "Uses custom music" };

    private CheckBox yycCheckBox = new CheckBox() { Text = "Uses the YoYo Compiler" };

    private CheckBox windowsCheckBox = new CheckBox() { Text = "Supports Windows" };
    private Button windowsButton = new Button() { Text = "Load modded Windows .zip", Enabled = false };
    private Label windowsLabel = new Label() { Text = "Modded Windows game loaded!", Visible = false };
    
    private CheckBox apkCheckBox = new CheckBox() { Text = "Additionally bundle Android" };
    private Button apkButton = new Button() { Text = "Load modded Android APK", Enabled = false };
    private Label apkLabel = new Label() { Text = "Modded APK loaded!", Visible = false };

    private CheckBox linuxCheckBox = new CheckBox() { Text = "Supports Linux" };
    private Button linuxButton = new Button() { Text = "Load modded Linux .zip", Enabled = false };
    private Label linuxLabel = new Label() { Text = "Modded Linux game loaded!", Visible = false };
    
    private CheckBox macCheckBox = new CheckBox() { Text = "Supports Mac" };
    private Button macButton = new Button() { Text = "Load modded Mac .zip", Enabled = false };
    private Label macLabel = new Label() { Text = "Modded Mac game loaded!", Visible = false };

    private Button originalZipButton = new Button() { Text = "Load 1.1" };
    private Label originalZipLabel = new Label() { Text = "1.1 loaded!", Visible = false};
    private Button createButton = new Button() { Text = "Create mod package(s)", Enabled = false };
    private Label createLabel = new Label() { Text = "Mod package created!", Visible = false};
    #endregion
}