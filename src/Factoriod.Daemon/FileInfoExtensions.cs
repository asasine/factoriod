namespace Factoriod.Daemon
{
    public static class FileInfoExtensions
    {
        public static FileInfo ResolveTilde(this FileInfo fileInfo)
            => new(fileInfo.ToString().Replace("~", Environment.GetEnvironmentVariable("HOME")));
    }
}
