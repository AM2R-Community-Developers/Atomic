using System;
using System.Xml.Serialization;

namespace AM2R_ModPacker
{
    /// <summary>
    /// Class that handles how the modmeta settings are saved as XML.
    /// </summary>
    [Serializable]
    [XmlRoot("message")]
    public class ModProfileXML
    {
        [XmlAttribute("Version")]
        public decimal Version
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

        [XmlAttribute("OperatingSystem")]
        public string OperatingSystem
        { get; set; }

        public ModProfileXML()
        { }

        public ModProfileXML(decimal version, string name, string author, bool usesCustomMusic, string saveLocation, bool android, bool usesYYC, string operatingSystem)
        {
            Version = version;
            Name = name;
            Author = author;
            UsesCustomMusic = usesCustomMusic;
            SaveLocation = saveLocation;
            Android = android;
            UsesYYC = usesYYC;
            OperatingSystem = operatingSystem;
        }
    }
}
