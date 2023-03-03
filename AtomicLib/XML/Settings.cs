using System.Xml.Serialization;

namespace AtomicLib.XML;

[Serializable]
[XmlRoot("message")]
public class Settings
{
    [XmlAttribute("Language")]
    public string Language
    { get; set; }
    
    [XmlAttribute("FillInContents")]
    public bool FillInContents
    { get; set; }

    public Settings()
    // default settings
    {
        Language = "English";
        FillInContents = false;
    }
}