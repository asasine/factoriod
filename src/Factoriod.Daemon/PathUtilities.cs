namespace Factoriod.Daemon
{
    public static class PathUtilities
    {
        public static string ResolveTilde(string path)
            => path.Replace("~", Environment.GetEnvironmentVariable("HOME"));
    }
}
