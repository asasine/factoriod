namespace Factoriod.Utilities
{
    public static class PathUtilities
    {
        /// <summary>
        /// Resolve a path into a fully-qualified path.
        /// 
        /// The special character ~ is resolved into <see cref="Environment.SpecialFolder.UserProfile"/>.
        /// </summary>
        /// <param name="path">The path to resolve.</param>
        /// <returns>The resolved path.</returns>
        public static string Resolve(string path)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (path.StartsWith('~') && home != null)
            {
                path = Path.Combine(home, Path.GetRelativePath("~", path));
            }

            if (!Path.IsPathFullyQualified(path))
            {
                path = Path.GetFullPath(path);
            }

            return path;
        }

        public static DirectoryInfo Resolve(this DirectoryInfo directory)
            => new(Resolve(directory.ToString()));

        public static FileInfo Resolve(this FileInfo file)
            => new(Resolve(file.ToString()));
    }
}
