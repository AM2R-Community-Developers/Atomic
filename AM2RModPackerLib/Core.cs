using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace AM2RModPackerLib;

public enum ProfileOperatingSystems
{
    Windows,
    Linux,
    Mac
}

public static class Core
{
    public static readonly string Version = "2.0.3";
    
    private static readonly string localPath = Directory.GetCurrentDirectory();
    
    

    
    public static void CreatePatch(string original, string modified, string output)
    {
        // Specify process start info
        var parameters = new ProcessStartInfo
        {
            // TODO: deal with linux/mac 
            FileName = localPath + "/utilities/xdelta/xdelta3.exe",
            WorkingDirectory = localPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = "-f -e -s \"" + original + "\" \"" + modified + "\" \"" + output + "\""
        };

        // Launch process and wait for exit. using statement automatically disposes the object for us!
        using var proc = new Process { StartInfo = parameters };
        proc.Start();
        proc.WaitForExit();
    }
    
    public static string CalculateMD5(string filename)
    {
        using var stream = File.OpenRead(filename);
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
    
    public static void CopyFilesRecursive(DirectoryInfo source, string[] blacklist, string destination)
    {
        foreach (var file in source.GetFiles())
        {
            if (!blacklist.Contains(file.Name))
                file.CopyTo(destination + "/" + file.Name);
        }

        foreach (var dir in source.GetDirectories())
        {
            // Folders need to be lowercase, because GM only reads from lowercase names on *nix systems. Windows is case-insensitive so doesnt matter for them
            string newDir = Directory.CreateDirectory(destination + "/" + dir.Name.ToLower()).FullName;
            CopyFilesRecursive(dir, blacklist, newDir);
        }
    }
}