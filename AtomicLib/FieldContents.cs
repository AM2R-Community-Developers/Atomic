using System.Xml.Serialization;

namespace AtomicLib;

/// <summary>
/// Class that handles how field information is saved as XML.
/// </summary>
[Serializable]
[XmlRoot("fieldcontents")]

public class FieldContents
{
    /// <summary>The text entered into a field for the mod name.</summary>
    [XmlAttribute("ModName")]
    public string ModName
    { get; set; }
    /// <summary>The text entered into a field for the mod author.</summary>
    [XmlAttribute("Author")]
    public string Author
    { get; set; }
    /// <summary>The text entered into a field for the mod version.</summary>
    [XmlAttribute("Version")]
    public string Version
    { get; set; }
    /// <summary>The text entered into a field for the mod notes.</summary>
    [XmlAttribute("Notes")]
    public string Notes
    { get; set; }
    /// <summary>The checkbox that indicates if the mod uses custom saves.</summary>
    [XmlAttribute("UsesCustomSave")]
    public bool UsesCustomSave
    { get; set; }
    /// <summary>The text that indicates the path used to store the mod's custom saves.</summary>
    [XmlAttribute("CustomSaveDir")]
    public string CustomSaveDir
    { get; set; }
    /// <summary>The checkbox that indicates if the mod uses custom music.</summary>
    [XmlAttribute("UsesCustomMusic")]
    public bool UsesCustomMusic
    { get; set; }
    /// <summary>The checkbox that indicates if the mod uses the YoYo Compiler.</summary>
    [XmlAttribute("UsesYYC")]
    public bool UsesYYC
    { get; set; }
    /// <summary>The checkbox that indicates if the mod supports Windows</summary>
    [XmlAttribute("SupportsWindows")]
    public bool SupportsWindows
    { get; set; }
    /// <summary>The checkbox that indicates if the mod supports Linux</summary>
    [XmlAttribute("SupportsLinux")]
    public bool SupportsLinux
    { get; set; }
    /// <summary>The checkbox that indicates if the mod supports Mac</summary>
    [XmlAttribute("SupportsMac")]
    public bool SupportsMac
    { get; set; }
    /// <summary>The checkbox that indicates if the mod supports Android</summary>
    [XmlAttribute("SupportsAndroid")]
    public bool SupportsAndroid
    { get; set; }

    public FieldContents()
    {
        
    }
}