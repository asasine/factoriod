namespace Factoriod.Fetcher
{
    public readonly record struct ReleaseBuild
    {
        public static readonly ReleaseBuild Alpha = new("alpha");
        public static readonly ReleaseBuild Demo = new("demo");
        public static readonly ReleaseBuild Headless = new("headless");

        private readonly string value;
        public ReleaseBuild(string value) => this.value = value.ToLower();
        public override string ToString() => this.value;
    }
}
