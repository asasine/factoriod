namespace Factoriod.Fetcher
{
    public readonly struct Distro
    {
        public static readonly Distro Win64 = new("win64");
        public static readonly Distro Win64Manual = new("win64-manual");
        public static readonly Distro Win32 = new("win32");
        public static readonly Distro Win32Manual = new("win32-manual");
        public static readonly Distro OSX = new("osx");
        public static readonly Distro Linux64 = new("linux64");
        public static readonly Distro Linux32 = new("linux32");

        private readonly string value;
        private Distro(string value) => this.value = value;
        public override string ToString() => this.value;
    }
}
