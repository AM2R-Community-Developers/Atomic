using System.Xml.Serialization;
using AtomicLib.XML;

namespace AtomicLib;

/// <summary>
/// Class that handles saving and loading the modpacker configuration.
/// </summary>
[Serializable]
[XmlRoot("config")]
public class Config
{
    public static readonly string ConfigFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "Atomic", "config.xml");
    
    /// <summary>
    /// Language used for the modpacker.
    /// </summary>
    [XmlAttribute("Language")]
    public string Language
    { get; set; }
    
    /// <summary>
    /// Determines if the modpacker should remember information entered into the fields between usages.
    /// </summary>
    [XmlAttribute("FillInContents")]
    public bool FillInContents
    { get; set; }
    
    /// <summary>
    /// Used to save information from the fields. Only used when <see cref="FillInContents"/> is <see langword="true"/>
    /// </summary>
    [XmlElement("Fields")]
    public FieldContents Fields
    { get; set; }
    
    public Config()
    {

    }

    public Config(string language, bool fillIn)
    {
        Language = language;
        FillInContents = fillIn;
        Fields = new FieldContents();
    }
    
    /// <summary>
    /// Reads the configuration from disk and returns a <see cref="Config"/> object. If the configuration does not exist, a default configuration will be created.
    /// </summary>
    /// <returns> Returns a <see cref="Config"/> object containing either the configuration read from disk or the default one.</returns>
    public static Config LoadAndReturnConfig()
    {
        if (!File.Exists(ConfigFilePath))
            return CreateAndReturnDefaultConfig(); 
                    
        return Serializer.Deserialize<Config>(File.ReadAllText(ConfigFilePath));     
    }
    
    /// <summary>
    /// Saves a default configuration to disk and returns it, creating the config folders if necessary.
    /// </summary>
    /// <returns> Returns the <see cref="Config"/> object.</returns>
    public static Config CreateAndReturnDefaultConfig()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
        Config defaultConfig = new Config("SystemLanguage", true);
        SaveConfig(defaultConfig);
        return defaultConfig;
    }
    
    /// <summary>
    /// Writes the <see cref="Config"/> to disk.
    /// </summary>
    /// <param name="config">The <see cref="Config"/> that should be saved to the disk.</param>
    public static void SaveConfig(Config config)
    {
        string xmlOutput = Serializer.Serialize<Config>(config);
        File.WriteAllText(ConfigFilePath, xmlOutput);
    }
}