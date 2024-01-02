using System;
using System.Collections.Generic;
using Atomic.Language;
using AtomicLib;
using Eto.Drawing;
using Eto.Forms;

namespace Atomic;

public class SettingsForm : Dialog
{
    public SettingsForm(Config config)
    {
        currentConfig = config;
        Title = Text.SettingsTitle;
        Icon = new Icon(1f, new Bitmap(Resources.icon64));
        // TODO: "eto bug" where this dialog behaves very weirdly if you try to resize it. As if the actual size is bigger than the display size
        Resizable = false;

        var layout = new DynamicLayout() { Padding = 10, Spacing = new Size(20, 20) };
        
        List<string> languageList = new List<string>
        {
            Text.SystemLanguage,
            "Deutsch",
            "English",
            "Español",
            "Français",
            "Italiano",
            "Português",
            "Русский",
            "日本語",
            "中文(简体)"
        };

        languageDropDown = new DropDown() { DataStore = languageList };
        languageDropDown.SelectedKey = currentConfig.Language == "SystemLanguage" ? Text.SystemLanguage : currentConfig.Language;

        fillInContents = new CheckBox() { Text = Text.RememberFields };
        fillInContents.Checked = currentConfig.FillInContents;

        layout.AddRange(Text.LanguageNotice, languageDropDown, fillInContents);
        
        Content = layout;
        
        this.Closing += SettingsForm_Closing;
    }

    private void SettingsForm_Closing(object sender, EventArgs e)
    {
        currentConfig.Language = languageDropDown.SelectedKey == Text.SystemLanguage ? "SystemLanguage" : languageDropDown.SelectedKey;
        currentConfig.FillInContents = fillInContents.Checked.Value;
        Config.SaveConfig(currentConfig);
    }

    private Config currentConfig;
    private DropDown languageDropDown;
    private CheckBox fillInContents;

}