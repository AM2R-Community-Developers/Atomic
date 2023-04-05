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
        Resizable = true;

        var layout = new DynamicLayout() { Padding = 10, Spacing = new Size(20, 20) };
        
        List<String> languageList = new List<String>
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

        this.Closing += SettingsForm_Closing;
        
        layout.AddRange(Text.LanguageNotice, languageDropDown, fillInContents);
        //layout.AddSpace();
        
        Content = layout;
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