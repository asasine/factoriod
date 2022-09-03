namespace Factoriod.Models;

public readonly record struct Save(string Path)
{

    private readonly Lazy<string> name = new(() => GetSaveName(Path));
    public string Name => name.Value;
    public DateTimeOffset LastWriteTime => new(File.GetLastWriteTimeUtc(Path), TimeSpan.Zero);

    private static string GetSaveName(string file) => System.IO.Path.GetFileNameWithoutExtension(file);
}
