using System.IO.Compression;

namespace Factoriod.Models.Game;

public static class MapExchangeString
{
    public static void Parse(string exchangeString)
    {
        exchangeString = exchangeString.ReplaceLineEndings(string.Empty); // Remove line endings
        if (!exchangeString.StartsWith(">>>") || !exchangeString.EndsWith("<<<"))
        {
            throw new ArgumentException("Invalid exchange string: must start with >>> and end with <<<", nameof(exchangeString));
        }

        exchangeString = exchangeString[3..^3]; // Remove >>> and <<< from the string
        var bytes = Convert.FromBase64String(exchangeString);
        using var memoryStream = new MemoryStream(bytes);
        using var zlibStream = new ZLibStream(memoryStream, CompressionMode.Decompress);

        // convert entire stream to hex bytes string
        // using var ms = new MemoryStream();
        // zlibStream.CopyTo(ms);
        // ms.Position = 0;
        // var hex = BitConverter.ToString(ms.ToArray());
        // Console.WriteLine(hex);
        // return;

        using var reader = new FactorioBinaryReader(zlibStream);

        var version = reader.ReadVersion();
        Console.WriteLine($"version: {version}");

        Console.WriteLine(reader.ReadByte()); // unknown

        var terrainSegmentation = reader.ReadSingle();
        Console.WriteLine($"terrainSegmentation: {terrainSegmentation}");

        var water = reader.ReadSingle();
        Console.WriteLine($"water: {water}");

        var autoplaceControls = reader.ReadAutoplaceControls();
        Console.WriteLine($"autoplaceControls: {autoplaceControls}");

        var autoplaceSettings = reader.ReadAutoplaceSettings();
        Console.WriteLine($"autoplaceSettings: {autoplaceSettings}");

        var defaultEnableAllAutoplaceControls = reader.ReadBoolean();
        Console.WriteLine($"defaultEnableAllAutoplaceControls: {defaultEnableAllAutoplaceControls}");

        var seed = reader.ReadSeed();
        Console.WriteLine($"seed: {seed}");

        var width = reader.ReadWidth();
        Console.WriteLine($"width: {width}");

        var height = reader.ReadHeight();
        Console.WriteLine($"height: {height}");

        var startingArea = reader.ReadByte();
        Console.WriteLine($"startingArea: {startingArea}");
    }
}
