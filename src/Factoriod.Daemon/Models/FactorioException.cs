using Factoriod.Models;

namespace Factoriod.Daemon.Models;

[Serializable]
public class FactorioException : Exception
{
    public FactorioException() { }
    public FactorioException(string message) : base(message) { }
    public FactorioException(string message, Exception inner) : base(message, inner) { }
    protected FactorioException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class IncompatibleMapVersionException : FactorioException
{
    public Version OldVersion { get; }
    public Version NewVersion { get; }
    public Save? Save { get; }

    public IncompatibleMapVersionException(Version oldVersion, Version newVersion, Save? save)
        : base(GenerateMessage(oldVersion, newVersion, save))
    {
        OldVersion = oldVersion;
        NewVersion = newVersion;
        Save = save;
    }

    private static string GenerateMessage(Version oldVersion, Version newVersion, Save? save)
    {
        if (save.HasValue)
        {
            return $"Save file is from a newer version {newVersion} than the downloaded executable {oldVersion}: {save.Value.Path}";
        }
        else
        {
            return $"Save file is from a newer version {newVersion} than the downloaded executable {oldVersion}";
        }
    }
}
