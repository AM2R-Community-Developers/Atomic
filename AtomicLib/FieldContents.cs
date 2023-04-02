using System.Reflection;
using System.Xml.Serialization;
using AtomicLib.XML;

namespace AtomicLib;

[Serializable]
[XmlRoot("fieldcontents")]

public class FieldContents
{
    
    [XmlAttribute("ModName")]
    public string ModName
    { get; set; }
    
    [XmlAttribute("Author")]
    public string Author
    { get; set; }
    
    [XmlAttribute("Version")]
    public string Version
    { get; set; }
    
    [XmlAttribute("Notes")]
    public string Notes
    { get; set; }
    
    [XmlAttribute("UsesCustomSave")]
    public bool UsesCustomSave
    { get; set; }
    
    [XmlAttribute("CustomSaveDir")]
    public string CustomSaveDir
    { get; set; }
    
    [XmlAttribute("UsesCustomMusic")]
    public bool UsesCustomMusic
    { get; set; }
    
    [XmlAttribute("UsesYYC")]
    public bool UsesYYC
    { get; set; }
    
    [XmlAttribute("SupportsWindows")]
    public bool SupportsWindows
    { get; set; }
    
    [XmlAttribute("SupportsLinux")]
    public bool SupportsLinux
    { get; set; }
    
    [XmlAttribute("SupportsMac")]
    public bool SupportsMac
    { get; set; }
    
    [XmlAttribute("SupportsAndroid")]
    public bool SupportsAndroid
    { get; set; }

    public FieldContents()
    {
        
    }
}