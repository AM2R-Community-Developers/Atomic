using System.Reflection;
using System.Xml.Serialization;
using AtomicLib.XML;

namespace AtomicLib;

[Serializable]
[XmlRoot("message")]

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
    
    [XmlAttribute("CustomSave")]
    public bool UsesCustomSave
    { get; set; }
    
    [XmlAttribute("CustomSaveDir")]
    public string CustomSaveDir
    { get; set; }
    
    [XmlAttribute("CustomMusic")]
    public bool UsesCustomMusic
    { get; set; }
    
    [XmlAttribute("YYC")]
    public bool UsesYYC
    { get; set; }
    
    [XmlAttribute("Windows")]
    public bool SupportsWindows
    { get; set; }
    
    [XmlAttribute("Linux")]
    public bool SupportsLinux
    { get; set; }
    
    [XmlAttribute("Mac")]
    public bool SupportsMac
    { get; set; }
    
    [XmlAttribute("Android")]
    public bool SupportsAndroid
    { get; set; }

    public FieldContents()
    {
        
    }
}