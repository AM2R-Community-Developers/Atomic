using System;
using System.Collections.Generic;
using Atomic.Language;
using AtomicLib;
using AtomicLib.XML;
using Eto.Drawing;
using Eto.Forms;

namespace Atomic;

public class SettingsForm : Dialog
{

    public SettingsForm(Config currentConfig)
    {
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

        var langdropDown = new DropDown() { DataStore = languageList };
        langdropDown.SelectedKey = currentConfig.Language;

        var fillInContents = new CheckBox() { Text = Text.RememberFields };
        fillInContents.Checked = currentConfig.FillInContents;

        this.Closing += (sender, args) =>
        {
            currentConfig.Language = langdropDown.SelectedKey;
            currentConfig.FillInContents = fillInContents.Checked.Value;
            Config.SaveConfig(currentConfig);
        };
        

        layout.AddRange(Text.LanguageNotice, langdropDown, fillInContents);
        //layout.AddSpace();
        
        Content = layout;
    }
}