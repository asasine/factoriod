namespace Factoriod.Utilities.Test;

public class PathUtilitiesTests
{
    [Fact]
    public void ResolveTildeString()
    {
        var paths = new string[]
        {
            
        };
    }

    [Fact]
    public void ResolveTildeRelativeBecomesAbsolute()
    {
        var path = "subdirectory/file.txt";
        Assert.False(Path.IsPathFullyQualified(path));
        
        var resolved = PathUtilities.ResolveTilde(path);
        Assert.True(Path.IsPathFullyQualified(resolved));
    }

    [Fact]
    public void ResolveTildeDirectoryInfo()
    {

    }

    [Fact]
    public void ResolveTildeFileInfo()
    {

    }
}
