using System.Xml.Serialization;
using AtomicLib.XML;

namespace AtomicLib;

[Serializable]
[XmlRoot("message")]
public class Config
{
    public static string ConfigFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atomic/config.xml");
    
    [XmlAttribute("Language")]
    public string Language
    { get; set; }
    
    [XmlAttribute("FillInContents")]
    public bool FillInContents
    { get; set; }

    // default settings
    public Config()
    {

    }

    public Config(string language, bool fillIn)
    {
        Language = language;
        FillInContents = fillIn;
    }
    
    // functions
    public static Config LoadAndReturnConfig()
    {
        Config config = null;
        if (File.Exists(ConfigFilePath))
        {
            string configXml = File.ReadAllText(ConfigFilePath);
            config = Serializer.Deserialize<Config>(configXml);
        }
        else
        {
            config = CreateAndReturnDefaultConfig();
        }
        return config;
    }
    
    public static Config CreateAndReturnDefaultConfig()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
        Config defaultConfig = new Config("English", false);
        SaveConfig(defaultConfig);
        return defaultConfig;
    }
    
    public static void SaveConfig(Config config)
    {
        string xmlOutput = Serializer.Serialize<Config>(config);
        File.WriteAllText(ConfigFilePath, xmlOutput);
    }
}