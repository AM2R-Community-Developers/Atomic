using System;
using System.Xml.Serialization;

namespace AM2RModPackerLib.XML;

//TODO: sync this with the launcher xml, a bunch more comments are in there.
/// <summary>
/// Class that handles how the modmeta settings are saved as XML.
/// </summary>
[Serializable]
[XmlRoot("message")]
public class ModProfileXML
{
    [XmlAttribute("OperatingSystem")]
    public string OperatingSystem
    { get; set; }

    [XmlAttribute("XMLVersion")]
    public int XMLVersion
    { get; set; }

    [XmlAttribute("Version")]
    public string Version
    { get; set; }

    [XmlAttribute("Name")]
    public string Name
    { get; set; }

    [XmlAttribute("Author")]
    public string Author
    { get; set; }

    [XmlAttribute("UsesCustomMusic")]
    public bool UsesCustomMusic
    { get; set; }

    [XmlAttribute("SaveLocation")]
    public string SaveLocation
    { get; set; }

    [XmlAttribute("SupportsAndroid")]
    public bool Android
    { get; set; }

    [XmlAttribute("UsesYYC")]
    public bool UsesYYC
    { get; set; }

    [XmlAttribute("Installable")]
    public bool Installable
    { get; set; }

    /// <summary>Indicates any notes that the mod author deemed worthy to share about his mod.</summary>
    [XmlAttribute("ProfileNotes")]
    public string ProfileNotes
    { get; set; }

    // TODO: this should fill in sensible default values
    public ModProfileXML()
    { }

    public ModProfileXML(string operatingSystem, int xmlVersion, string version, string name, string author, bool usesCustomMusic, string saveLocation, bool android, bool usesYYC, string profileNotes, bool installable = true)
    {
        OperatingSystem = operatingSystem;
        XMLVersion = xmlVersion;
        Version = version;
        Name = name;
        Author = author;
        UsesCustomMusic = usesCustomMusic;
        SaveLocation = saveLocation;
        Android = android;
        UsesYYC = usesYYC;
        ProfileNotes = profileNotes;
        Installable = installable;
    }
}