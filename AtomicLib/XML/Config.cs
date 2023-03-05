using System.Xml.Serialization;

namespace AtomicLib.XML;

[Serializable]
[XmlRoot("message")]
public class Config
{
    [XmlAttribute("Language")]
    public string Language
    { get; set; }
    
    [XmlAttribute("FillInContents")]
    public bool FillInContents
    { get; set; }

    // default settings
    public Config()
    {
        Language = "English";
        FillInContents = false;
    }
}