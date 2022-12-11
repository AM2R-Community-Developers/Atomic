using System.Reflection;
using AM2RModPackerLib.XML;

namespace AM2RModPackerLib;

public class ModCreationInfo
{
    public ModProfileXML profile;

    public bool IsAM2R11Loaded => String.IsNullOrWhiteSpace(originalPath);
    public bool IsWindowsModLoaded=> String.IsNullOrWhiteSpace(windowsPath);
    public bool IsApkModLoaded => String.IsNullOrWhiteSpace(apkPath);
    public bool IsLinuxModLoaded=> String.IsNullOrWhiteSpace(linuxPath);
    public bool IsMacModLoaded=> String.IsNullOrWhiteSpace(macPath);
    
    public string originalPath, windowsPath, apkPath, linuxPath, macPath;
    public string saveFilePath;
}