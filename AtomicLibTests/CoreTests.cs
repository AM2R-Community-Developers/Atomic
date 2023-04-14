using System.IO.Compression;
using System.Xml.Serialization;
using AtomicLib;
using AtomicLib.XML;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AtomicLibTests;

public class CoreTests : IDisposable
{
    private string am2r_11Path;
    private ITestOutputHelper console;
    private string testTempDir;
    
    public CoreTests(ITestOutputHelper output)
    {
        console = output;
        testTempDir = Path.GetTempPath() + Guid.NewGuid() + "/";
        // TODO: use a custom "AM2R_11" which is just a modified am2r_sever. the backend doesnt check for 1.1 validity, so we can do that.
        Directory.CreateDirectory(testTempDir);
        var am2rEnvVar = Environment.GetEnvironmentVariable("AM2R_11PATH");
        if (am2rEnvVar is not null && File.Exists(am2rEnvVar))
        {
            am2r_11Path = am2rEnvVar;
            return;
        }

        if (File.Exists("AM2R_11.zip"))
        {
            am2r_11Path = "AM2R_11.zip";
            return;
        }
        
        Assert.Fail("AM2R 1.1 file could not be found! Please place it into PWD or provide it via the AM2R_11PATH environment variable.");
    }

    public void Dispose()
    {
        Directory.Delete(testTempDir, true);
    }
    
    [Fact]
    public void CreateModPack_ShouldThrowWithNullModInfo()
    {
        Assert.Throws<NullReferenceException>(() => Core.CreateModPack(null, testTempDir + "foo.zip"));
    }
    
    [Fact]
    public void CreateModPack_ShouldThrowWithNullModInfoProfile()
    {
        var modInfo = new ModCreationInfo();
        modInfo.Profile = null;
        Assert.Throws<NullReferenceException>(() => Core.CreateModPack(modInfo, testTempDir + "foo.zip"));
    }
    
    [Fact]
    public void CreateModPack_ShouldThrowWithAM2R11PathNotSet()
    {
        var modInfo = new ModCreationInfo();
        modInfo.Profile = new ModProfileXML();
        modInfo.Profile.OperatingSystem = "Windows";
        
        Assert.Throws<FileNotFoundException>(() => Core.CreateModPack(modInfo, testTempDir + "foo.zip"));
    }
    
    [Fact]
    public void CreateModPack_ShouldThrowWithUnknownProfileOS()
    {
        var modInfo = new ModCreationInfo();
        modInfo.Profile = new ModProfileXML();
        modInfo.Profile.OperatingSystem = "asdfasdf";
        modInfo.AM2R11Path = am2r_11Path;
        
        Assert.Throws<ArgumentException>(() => Core.CreateModPack(modInfo, testTempDir + "foo.zip"));
    }
    
    [Fact]
    public void CreateModPack_ShouldThrowWhenProfileSetButNotOSpath()
    {
        var modInfo = new ModCreationInfo();
        modInfo.Profile = new ModProfileXML();
        modInfo.Profile.OperatingSystem = "Windows";
        modInfo.AM2R11Path = am2r_11Path;
        
        Assert.Throws<FileNotFoundException>(() => Core.CreateModPack(modInfo, testTempDir + "foo.zip"));
    }
    
    [Fact]
    public void CreateModPack_ShouldThrowWhenAndroidIsMarkedAsSupportedButPathDoesNotExist()
    {
        var modInfo = new ModCreationInfo();
        modInfo.Profile = new ModProfileXML();
        modInfo.Profile.OperatingSystem = "Windows";
        modInfo.Profile.SupportsAndroid = true;
        modInfo.AM2R11Path = am2r_11Path;
        
        Assert.Throws<FileNotFoundException>(() => Core.CreateModPack(modInfo, testTempDir + "foo.zip"));
    }
    
    [Theory]
    [InlineData("Windows", false, false, false)]
    [InlineData("Windows", false, false, true)]
    [InlineData("Windows", false, true, false)]
    [InlineData("Windows", false, true, true)]
    [InlineData("Windows", true, false, false)]
    [InlineData("Windows", true, false, true)]
    [InlineData("Windows", true, true, false)]
    [InlineData("Windows", true, true, true)]
    [InlineData("Linux", false, false, false)]
    [InlineData("Linux", false, false, true)]
    [InlineData("Linux", false, true, false)]
    [InlineData("Linux", false, true, true)]
    [InlineData("Linux", true, false, false)]
    [InlineData("Linux", true, false, true)]
    [InlineData("Linux", true, true, false)]
    [InlineData("Linux", true, true, true)]
    [InlineData("Mac", false, false, false)]
    [InlineData("Mac", false, false, true)]
    [InlineData("Mac", false, true, false)]
    [InlineData("Mac", false, true, true)]
    [InlineData("Mac", true, false, false)]
    [InlineData("Mac", true, false, true)]
    [InlineData("Mac", true, true, false)]
    [InlineData("Mac", true, true, true)]
    public void CreateModPack_AllOptionsShouldCauseValidModpacks(string operatingSystem, bool usesCustomMusic, bool supportsAndroid, bool isYYC)
    {
        var modInfo = new ModCreationInfo();
        modInfo.Profile = new ModProfileXML();
        modInfo.Profile.OperatingSystem = operatingSystem;
        modInfo.Profile.UsesCustomMusic = usesCustomMusic;
        modInfo.Profile.SupportsAndroid = supportsAndroid;
        if (supportsAndroid)
            modInfo.ApkModPath = "GameAndroid.apk";
        modInfo.Profile.UsesYYC = isYYC;
        modInfo.Profile.Name = "Cool Mod";
        modInfo.Profile.Version = "cool version";
        modInfo.Profile.ProfileNotes = "This is my very own cool mod";
        modInfo.AM2R11Path = am2r_11Path;
        switch (operatingSystem)
        {
            case "Windows": modInfo.WindowsModPath = "GameWin.zip"; break;
            case "Linux": modInfo.LinuxModPath = "GameLin.zip"; break;
            case "Mac": modInfo.MacModPath = "GameMac.zip"; break;
            default: Assert.Fail(nameof(CreateModPack_AllOptionsShouldCauseValidModpacks) + " was called with improper parameters?"); break;
        }
        
        Core.CreateModPack(modInfo, testTempDir + "foo.zip");

        // TODO: assert on proper packaging, by investigating contents of zip
        ZipArchive archive = ZipFile.OpenRead(testTempDir + "foo.zip");
        Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "profile.xml") is not null);
        // TODO: try to deserialize xml to check it has what we put in
        if (isYYC)
        {
            if (operatingSystem == "Windows")
            {
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "AM2R.xdelta") is not null);
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "game.xdelta") is null);
            }
        }
        else
        {
            // Unix has both in YYC and non-YYC
            Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "AM2R.xdelta") is not null);
            if (operatingSystem == "Windows")
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "data.xdelta") is not null);
            else
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "game.xdelta") is not null);
        }

        if (supportsAndroid)
        {
            Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "droid.xdelta") is not null);
            Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "android/AM2RWrapper.apk") is not null);
            if (isYYC)
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "android/AM2R.ini") is not null);
            // TODO: atomic currently always copies the file if it exists. Should it only copy it if we're dealing with yyc?
            //else
                //Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "android/AM2R.ini") is null);
        }

        if (operatingSystem is "Linux" or "Mac")
        {
            Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "files_to_copy/icon.png") is not null);
            Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "files_to_copy/splash.png") is not null);
            if (operatingSystem == "Mac")
            {
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "Info.plist") is not null);
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "files_to_copy/yoyorunner.config") is not null);
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "files_to_copy/gamecontrollerdb.txt") is not null);
                Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "files_to_copy/english.lproj/MainMenu.nib") is not null);
            }
        }
        else
        {
            Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "files_to_copy/icon.png") is null);
            Assert.True(archive.Entries.FirstOrDefault(f => f.FullName == "files_to_copy/splash.png") is null);
        }

        if (usesCustomMusic)
        {
            // TODO: check for custom music. do that after we provided a fake am2r_11.
        }
        else
        {
            // TODO: make sure that if we're providing lowercase songs, that they'll be also blacklisted.
        }
    }

    [Fact]
    public void CreateModPack_ShouldCleanUpDirectoryIfItExistedBefore()
    {
        var modInfo = new ModCreationInfo();
        modInfo.Profile = new ModProfileXML();
        modInfo.Profile.OperatingSystem = "Windows";
        modInfo.Profile.UsesCustomMusic = false;
        modInfo.Profile.SupportsAndroid = false;
        modInfo.Profile.UsesYYC = false;
        modInfo.Profile.Name = "Cool Mod";
        modInfo.Profile.Version = "cool version";
        modInfo.Profile.ProfileNotes = "This is my very own cool mod";
        modInfo.WindowsModPath = "GameWin.zip";
        modInfo.AM2R11Path = am2r_11Path;
        
        string tempPath = Path.GetTempPath() + "/Atomic";
        Directory.CreateDirectory(tempPath);
        Assert.True(Directory.Exists(tempPath));
        
        Core.CreateModPack(modInfo, testTempDir + "foo.zip");

        Assert.False(Directory.Exists(tempPath));
    }
    
    [Fact]
    public void CreateModPack_ShouldCleanUpOutputFileIfItExistedBefore()
    {
        var modInfo = new ModCreationInfo();
        modInfo.Profile = new ModProfileXML();
        modInfo.Profile.OperatingSystem = "Windows";
        modInfo.Profile.UsesCustomMusic = false;
        modInfo.Profile.SupportsAndroid = false;
        modInfo.Profile.UsesYYC = false;
        modInfo.Profile.Name = "Cool Mod";
        modInfo.Profile.Version = "cool version";
        modInfo.Profile.ProfileNotes = "This is my very own cool mod";
        modInfo.WindowsModPath = "GameWin.zip";
        modInfo.AM2R11Path = am2r_11Path;
        
        string tempPath = testTempDir + "foo.zip";
        File.WriteAllText(tempPath, "foobar");
        Assert.True(File.Exists(tempPath));
        
        Core.CreateModPack(modInfo, testTempDir + "foo.zip");

        Assert.True(File.Exists(tempPath));
        using var reader = new StreamReader(tempPath);
        Assert.False(reader.ReadLine() == "foobar");
    }
    
}