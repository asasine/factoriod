namespace Factoriod.Utilities.Test;

public class PathUtilitiesTests
{
    [Fact]
    public void ResolveRelativeBecomesAbsolute()
    {
        var path = Path.Combine("foo", "bar", "baz.txt");
        Assert.False(Path.IsPathFullyQualified(path));
        
        var actual = PathUtilities.Resolve(path);
        Assert.True(Path.IsPathFullyQualified(actual));
    }

    [Fact]
    public void ResolveAbsoluteRemainsAbsolute()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "foo", "bar", "baz.txt");
        Assert.True(Path.IsPathFullyQualified(path));

        var actual = PathUtilities.Resolve(path);
        Assert.True(Path.IsPathFullyQualified(path));
        Assert.Equal(path, actual);
    }

    [Fact]
    public void ResolveHome()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expected = Path.Combine(home, "foo", "bar", "baz.txt");
        var path = "~/foo/bar/baz.txt";

        var actual = PathUtilities.Resolve(path);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ResolveRelativeDirectoryInfoEqualsStringPath()
    {
        var path = Path.Combine("foo", "bar");
        var directory = new DirectoryInfo(path);
        Assert.Equal(PathUtilities.Resolve(path), directory.Resolve().FullName);
    }

    [Fact]
    public void ResolveAbsoluteDirectoryInfoIsUnchanged()
    {
        var path = Path.Combine("C:", "foo", "bar");
        var directory = new DirectoryInfo(path);
        Assert.Equal(directory.FullName, directory.Resolve().FullName);
    }

    [Fact]
    public void ResolveHomeDirectoryInfo()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(home, "foo", "bar");
        var directory = new DirectoryInfo(Path.Combine("~", "foo", "bar"));

        Assert.Equal(PathUtilities.Resolve(path), directory.Resolve().FullName);
    }

    [Fact]
    public void ResolveRelativeFileInfoEqualsStringPath()
    {
        var path = Path.Combine("foo", "bar", "baz.txt");
        var file = new FileInfo(path);
        Assert.Equal(PathUtilities.Resolve(path), file.Resolve().FullName);
    }

    [Fact]
    public void ResolveAbsoluteFileInfoIsUnchanged()
    {
        var path = Path.Combine("C:", "foo", "bar", "baz.txt");
        var file = new FileInfo(path);
        Assert.Equal(file.FullName, file.Resolve().FullName);
    }

    [Fact]
    public void ResolveHomeFileInfo()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(home, "foo", "bar", "baz.txt");
        var file = new FileInfo(Path.Combine("~", "foo", "bar", "baz.txt"));

        Assert.Equal(PathUtilities.Resolve(path), file.Resolve().FullName);
    }
}
