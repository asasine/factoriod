namespace Factoriod.Utilities
{
    public static class PathUtilities
    {
        public static string ResolveTilde(string path)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (path.StartsWith('~') && home != null)
            {
                return path.Insert(0, home);
            }

            return path;
        }

        public static DirectoryInfo ResolveTilde(this DirectoryInfo directory)
            => new(ResolveTilde(directory.FullName));

        public static FileInfo ResolveTilde(this FileInfo file)
            => new(ResolveTilde(file.FullName));
    }
}
