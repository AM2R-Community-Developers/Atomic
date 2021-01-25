namespace AM2RModPackerConsole
{
    internal class ModProfile
    {
        public int version { get; set; }
        public string name { get; set; }
        public string author { get; set; }
        public bool usesCustomMusic { get; set; }
        public string saveLocation { get; set; }
        public bool android { get; set; }
        public bool usesYYC { get; set; }
        public ModProfile(int version, string name, string author, bool usesCustomMusic, string saveLocation, bool android, bool usesYYC)
        {
            this.version = version;
            this.name = name;
            this.author = author;
            this.usesCustomMusic = usesCustomMusic;
            this.saveLocation = saveLocation;
            this.android = android;
            this.usesYYC = usesYYC;
        }
    }
}
