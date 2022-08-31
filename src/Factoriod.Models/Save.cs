namespace Factoriod.Models;

public readonly record struct Save(string Name, DateTimeOffset LastWriteTime)
{
    public Save(FileInfo file)
        : this(GetSaveName(file), new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero))
    {
    }

    public static string GetSaveName(FileInfo file)
    {
        var parts = file.Name.Split('.');
        return string.Join(".", parts.Take(parts.Length - 1));
    }
}
