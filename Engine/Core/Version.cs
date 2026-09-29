namespace MiMFa.Engine.Core
{
    public class Version
    {
        public int Major { get; }
        public int Minor { get; }
        public int Build { get; }
        public int Revision { get; }

        public Version(int major = 1, int minor = 0, int build = 0, int revision = 0)
        {
            Major = major;
            Minor = minor;
            Build = build;
            Revision = revision;
        }

        public int Compare(Version version)
        {
            if (Major != version.Major) return Major - version.Major;
            if (Minor != version.Minor) return Minor - version.Minor;
            if (Build != version.Build) return Build - version.Build;
            return Revision - version.Revision;
        }

        public bool Equals(Version version) => version != null && Compare(version) == 0;
        public bool GreaterThan(Version version) => Compare(version) > 0;
        public bool LessThan(Version version) => Compare(version) < 0;

        public override string ToString() => $"{Major}.{Minor}.{Build}.{Revision}";
    }
}
