using System.Reflection;
using AM2RModPackerLib.XML;

namespace AM2RModPackerLib;

public class ModCreationInfo
{
    public ModProfileXML Profile = new ModProfileXML();

    public bool IsAM2R11Loaded => String.IsNullOrWhiteSpace(AM2R11Path);
    public bool IsWindowsModLoaded=> String.IsNullOrWhiteSpace(WindowsModPath);
    public bool IsApkModLoaded => String.IsNullOrWhiteSpace(ApkModPath);
    public bool IsLinuxModLoaded=> String.IsNullOrWhiteSpace(LinuxModPath);
    public bool IsMacModLoaded=> String.IsNullOrWhiteSpace(MacModPath);

    public string AM2R11Path;
    public string WindowsModPath;
    public string ApkModPath;
    public string LinuxModPath;
    public string MacModPath;
}