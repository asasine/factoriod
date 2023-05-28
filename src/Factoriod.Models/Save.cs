namespace Factoriod.Models;

/// <summary>
/// A factorio saved game.
/// </summary>
/// <param name="Path">
/// The path to the saved game.
///
/// <para>
/// <paramref name="Path"/> must be a <see cref="string"/> because <see cref="FileInfo"/> is not serializable.
/// </para>
/// </param>
public readonly record struct Save(string Path)
{

    private readonly Lazy<string> name = new(() => GetSaveName(Path));
    public string Name => name.Value;
    public DateTimeOffset LastWriteTime => new(File.GetLastWriteTimeUtc(Path), TimeSpan.Zero);

    public bool IsBackup => Path.EndsWith(".bak");

    public FileInfo GetFileInfo() => new(Path);

    private static string GetSaveName(string file) => System.IO.Path.GetFileNameWithoutExtension(file);
}
