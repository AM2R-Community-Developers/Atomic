using Eto.Forms;
using Eto.Drawing;
using AM2RModPacker.XML;
using System.IO;

namespace AM2RModPacker;

public partial class ModPacker : Form
{
    public ModPacker()
    {
        profile = new ModProfileXML("", 1, "", "", "", false, "", false, false, ""); // (1, "", "", false, "default", false, false);
        isOriginalLoaded = false;
        isModLoaded = false;
        isApkLoaded = false;
        isLinuxLoaded = false;

        localPath = Directory.GetCurrentDirectory();
        originalPath = "";
        modPath = "";
        linuxPath = "";
        apkPath = "";

        Title = "AM2R ModPacker " + VERSION;
        // Currently broken as I don't know how to do this from Rider
        //Icon = Icon.FromResource("icon64.ico");

        MinimumSize = new Size(550, 430);

        var mainContent = new DynamicLayout() { Spacing = new Size(15, 15) };
        var leftSide = new DynamicLayout() { Padding = 10, Spacing = new Size(5, 5) };
        var rightSide = new DynamicLayout() { Padding = 10, Spacing = new Size(5, 5) };

        // Left side
        var modInfoPanel = new DynamicLayout() { Spacing = new Size(5, 5) };
        modInfoPanel.AddRow(nameLabel, NameTextBox);
        modInfoPanel.AddRow(authorLabel, AuthorTextBox);
        modInfoPanel.AddRow(versionLabel, versionTextBox);
        modInfoPanel.AddRow(modNotesLabel);

        var modNotesPanel = new DynamicLayout();
        modNotesPanel.AddRow(modNotesTextBox);

        // Combine together
        leftSide.AddRow(modInfoPanel);
        leftSide.AddRow(modNotesPanel);

        // Right Side
        var savePanel = new DynamicLayout();
        savePanel.AddRow(SaveCheckBox, null, winSaveButton);

        var saveTextPanel = new DynamicLayout();
        saveTextPanel.AddRow(saveTextBox);

        var miscOptionsPanel = new DynamicLayout() { Spacing = new Size(5, 5) };
        miscOptionsPanel.AddRow(MusicCheckBox);
        miscOptionsPanel.AddRow(YYCCheckBox);
        miscOptionsPanel.AddRow(AndroidCheckBox);
        miscOptionsPanel.AddRow(ApkButton, ApkLabel);
        miscOptionsPanel.AddRow(linuxCheckBox);
        miscOptionsPanel.AddRow(linuxButton, linuxLabel);

        var loadZipsPanel = new DynamicLayout() { Spacing = new Size(5, 5) };
        loadZipsPanel.AddRow(OriginalButton, null, ModButton);
        loadZipsPanel.AddRow(OriginalLabel, null, ModLabel);

        var resultPanel = new DynamicLayout() { Spacing = new Size(5, 5) };
        resultPanel.AddRow(CreateButton);
        resultPanel.AddRow(CreateLabel);

        // Combine together
        rightSide.AddRow(savePanel);
        rightSide.AddRow(saveTextPanel);
        rightSide.AddRow(miscOptionsPanel);
        rightSide.AddRow(new Label() { Text = "I am a spacer!", TextColor = this.BackgroundColor });
        rightSide.AddRow(loadZipsPanel);
        rightSide.AddRow(resultPanel);
        
        // Combine all into main panel and assign
        Splitter splitter = new Splitter() {};
        splitter.Panel1 = leftSide;
        splitter.Panel2 = rightSide;
        splitter.Panel1MinimumSize = 180;
        splitter.Panel2MinimumSize = 320;
        splitter.Orientation = Orientation.Horizontal;
        
        mainContent.Add(splitter);
        Content = mainContent;

        // Assign events
        OriginalButton.Click += OriginalButton_Click;
        ModButton.Click += ModButton_Click;
        CreateButton.Click += CreateButton_Click;
        ApkButton.Click += ApkButton_Click;
        AndroidCheckBox.CheckedChanged += AndroidCheckBox_CheckedChanged;
        SaveCheckBox.CheckedChanged += SaveCheckBox_CheckedChanged;
        linuxCheckBox.CheckedChanged += linuxCheckBox_CheckedChanged;
        linuxButton.Click += linuxButton_Click;
        winSaveButton.Click += CustomSaveDataButton_Click;
    }

    #region Design Elements
    private Label nameLabel = new Label() { Text = "Mod name:" };
    private TextBox NameTextBox = new TextBox();
    private Label authorLabel = new Label() { Text = "Author:" };
    private TextBox AuthorTextBox = new TextBox();
    private Label versionLabel = new Label() { Text = "Version:" };
    private TextBox versionTextBox = new TextBox();
    private Label modNotesLabel = new Label() { Text = "Mod notes:" };
    private TextArea modNotesTextBox = new TextArea() { };

    private CheckBox SaveCheckBox = new CheckBox() { Text = "Uses custom save directory" };
    private Button winSaveButton = new Button() { Text = "Select folder", Enabled = false };
    private TextBox saveTextBox = new TextBox();
    private CheckBox MusicCheckBox = new CheckBox() { Text = "Uses custom music" };

    private CheckBox YYCCheckBox = new CheckBox() { Text = "Uses the YoYo Compiler" };

    private CheckBox AndroidCheckBox = new CheckBox() { Text = "Supports Android" };
    private Button ApkButton = new Button() { Text = "Load modded Android APK", Enabled = false };
    private Label ApkLabel = new Label() { Text = "Modded APK loaded!", Visible = false };

    private CheckBox linuxCheckBox = new CheckBox() { Text = "Supports Linux" };
    private Button linuxButton = new Button() { Text = "Load modded Linux .zip", Enabled = false };
    private Label linuxLabel = new Label() { Text = "Modded Linux game loaded!", Visible = false };

    private Button OriginalButton = new Button() { Text = "Load 1.1" };
    private Label OriginalLabel = new Label() { Text = "1.1 loaded!", Visible = false};
    private Button ModButton = new Button() { Text = "Load modded game" };
    private Label ModLabel = new Label() { Text = "Mod loaded!", Visible = false };
    private Button CreateButton = new Button() { Text = "Create mod package(s)", Enabled = false };
    private Label CreateLabel = new Label() { Text = "Mod package created!", Visible = false};
    #endregion
}