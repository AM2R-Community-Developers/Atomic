using Eto.Forms;
using Eto.Drawing;
using AM2RModPackerLib.XML;
using System.IO;

namespace AM2RModPacker;

public partial class ModPacker : Form
{
    public ModPacker()
    {
        profile = new ModProfileXML();

        Title = "AM2R ModPacker " + version;
        //TODO: Currently broken as I don't know how to do this from Rider
        //Icon = Icon.FromResource("icon64.ico");

        MinimumSize = new Size(550, 400);

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
        miscOptionsPanel.AddRow(apkCheckBox);
        miscOptionsPanel.AddRow(apkButton, apkLabel);
        miscOptionsPanel.AddRow(linuxCheckBox);
        miscOptionsPanel.AddRow(linuxButton, linuxLabel);
        miscOptionsPanel.AddRow(macCheckBox);
        miscOptionsPanel.AddRow(macButton, macLabel);

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
        splitter.Panel2MinimumSize = 320;
        splitter.Orientation = Orientation.Horizontal;
        
        mainContent.Add(splitter);
        Content = mainContent;

        // Assign events
        originalZipButton.Click += OriginalZipButton_Click;
        createButton.Click += CreateButton_Click;
        windowsCheckBox.CheckedChanged += WindowsCheckBox_CheckedChanged;
        windowsButton.Click += WindowsButton_Click;
        apkButton.Click += ApkButton_Click;
        apkCheckBox.CheckedChanged += ApkCheckBoxCheckedChanged;
        customSaveCheckBox.CheckedChanged += CustomSaveCheckBoxChecked_Changed;
        linuxCheckBox.CheckedChanged += LinuxCheckBox_CheckedChanged;
        linuxButton.Click += LinuxButton_Click;
        macCheckBox.CheckedChanged += macCheckBox_CheckedChanged;
        macButton.Click += macButton_Click;
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
    private TextBox customSaveTextBox = new TextBox() { ReadOnly = true};
    private CheckBox musicCheckBox = new CheckBox() { Text = "Uses custom music" };

    private CheckBox yycCheckBox = new CheckBox() { Text = "Uses the YoYo Compiler" };

    private CheckBox windowsCheckBox = new CheckBox() { Text = "Supports Windows" };
    private Button windowsButton = new Button() { Text = "Load modded Windows .zip", Enabled = false };
    private Label windowsLabel = new Label() { Text = "Modded Windows game loaded!", Visible = false };
    
    private CheckBox apkCheckBox = new CheckBox() { Text = "Supports Android" };
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