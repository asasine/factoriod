using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Factoriod.Models.Game;

/// <summary>
/// Reads primitive data types as binary values, handling "space-optimized" values.
/// https://wiki.factorio.com/Data_types#Space_Optimized
/// </summary>
public class FactorioBinaryReader : BinaryReader
{
    public FactorioBinaryReader(Stream input)
        : base(input)
    {
        // check endianness
        if (!BitConverter.IsLittleEndian)
        {
            throw new NotSupportedException("Factorio binary files are little endian");
        }
    }

    /// <summary>
    /// Reads a boolean value from the current stream and advances the current position of the stream by one byte.
    /// A value of 1 is <see langword="true"/>, all other values are <see langword="false"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the byte is 1, otherwise <see langword="false"/></returns>
    public override bool ReadBoolean()
    {
        return base.ReadByte() == 1;
    }

    /// <summary>
    /// Reads a signed <see langword="short"/> value from the current stream.
    /// Space-optimized values are supported.
    /// If the value is greater than or equal to 0, and less than 255, the stream is advanced by one byte.
    /// Otherwise, the stream is advanced by three bytes.
    /// </summary>
    /// <returns>A 2-byte signed integer from the current stream.</returns>
    public override short ReadInt16()
    {
        var value = base.ReadByte();
        return value < 255 ? value : base.ReadInt16();
    }

    /// <summary>
    /// Reads an unsigned <see langword="short"/> value from the current stream.
    /// Space-optimized values are supported.
    /// If the value is less than 255, the stream is advanced by one byte.
    /// Otherwise, the stream is advanced by three bytes.
    /// </summary>
    /// <returns>A 2-byte unsigned integer from the current stream.</returns>
    public override ushort ReadUInt16()
    {
        var value = base.ReadByte();
        return value < 255 ? value : base.ReadUInt16();
    }

    /// <summary>
    /// Reads a signed <see langword="int"/> value from the current stream.
    /// Space-optimized values are supported.
    /// If the value is greater than or equal to 0, and less than 255, the stream is advanced by one byte.
    /// Otherwise, the stream is advanced by five bytes.
    /// </summary>
    /// <returns>A 4-byte signed integer from the current stream.</returns>
    public override int ReadInt32()
    {
        var value = base.ReadByte();
        return value < 255 ? value : base.ReadInt32();
    }

    /// <summary>
    /// Reads an unsigned <see langword="uint"/> value from the current stream.
    /// Space-optimized values are supported.
    /// If the value is less than 255, the stream is advanced by one byte.
    /// Otherwise, the stream is advanced by five bytes.
    /// </summary>
    /// <returns>A 4-byte unsigned integer from the current stream.</returns>
    public override uint ReadUInt32()
    {
        var value = base.ReadByte();
        return value < 255 ? value : base.ReadUInt32();
    }

    /// <summary>
    /// Reads a signed <see langword="long"/> value from the current stream.
    /// Space-optimized values are supported.
    /// If the value is greater than or equal to 0, and less than 255, the stream is advanced by one byte.
    /// Otherwise, the stream is advanced by nine bytes.
    /// </summary>
    /// <returns>An 8-byte signed integer from the current stream.</returns>
    public override long ReadInt64()
    {
        var value = base.ReadByte();
        return value < 255 ? value : base.ReadInt64();
    }


    /// <summary>
    /// Reads an unsigned <see langword="uint"/> value from the current stream.
    /// Space-optimized values are supported.
    /// If the value is less than 255, the stream is advanced by one byte.
    /// Otherwise, the stream is advanced by nine bytes.
    /// </summary>
    /// <returns>An 8-byte unsigned integer from the current stream.</returns>
    public override ulong ReadUInt64()
    {
        var value = base.ReadByte();
        return value < 255 ? value : base.ReadUInt64();
    }

    /// <summary>
    /// Reads a <see cref="Version"/> value from the current stream.
    /// The version is stored as four 2-byte signed integers.
    /// The stream is advanced by eight bytes.
    /// </summary>
    /// <returns>A <see cref="Version"/> value from the current stream.</returns>
    public Version ReadVersion() => new(base.ReadInt16(), base.ReadInt16(), base.ReadInt16(), base.ReadInt16());

    /// <summary>
    /// Reads a <see cref="string"/> value from the current stream.
    /// The string is stored as a 4-byte signed integer indicating the length of the string,
    /// followed by the bytes of the string encoded as UTF-8.
    /// </summary>
    /// <returns>A <see cref="string"/> value from the current stream.</returns>
    public override string ReadString()
    {
        var length = ReadUInt32();
        if (length > int.MaxValue)
        {
            throw new InvalidOperationException($"String length {length} is greater than {int.MaxValue}");
        }

        var bytes = ReadBytes((int)length);
        return Encoding.UTF8.GetString(bytes);
    }

    public IReadOnlyDictionary<string, AutoplaceControl> ReadAutoplaceControls()
    {
        var autoplaceControls = new Dictionary<string, AutoplaceControl>();
        var keyCount = ReadUInt32();
        for (var i = 0; i < keyCount; i++)
        {
            var key = ReadString();
            var autoplaceControl = ReadAutoplaceControl();
            if (!autoplaceControls.TryAdd(key, autoplaceControl))
            {
                throw new InvalidOperationException($"Failed to add {nameof(AutoplaceControl)} for key \"{key}\"");
            }
        }

        return autoplaceControls;
    }

    public IReadOnlyDictionary<string, AutoplaceSettings> ReadAutoplaceSettings()
    {
        var autoplaceSettings = new Dictionary<string, AutoplaceSettings>();
        var keyCount = ReadUInt32();
        for (var i = 0; i < keyCount; i++)
        {
            var key = ReadString();
            var singleAutoplaceSetting = ReadSingleAutoplaceSettings();
            if (!autoplaceSettings.TryAdd(key, singleAutoplaceSetting))
            {
                throw new InvalidOperationException($"Failed to add {nameof(AutoplaceSettings)} for key \"{key}\"");
            }
        }

        return autoplaceSettings;
    }

    public uint ReadSeed() => base.ReadUInt32();
    public uint ReadWidth() => base.ReadUInt32();
    public uint ReadHeight() => base.ReadUInt32();
    public AutoplaceControl ReadStartingArea() => ReadAutoplaceControl();

    private AutoplaceControl ReadAutoplaceControl()
    {
        var frequency = ReadSingle();
        var size = ReadSingle();
        var richness = ReadSingle();
        return new AutoplaceControl(frequency, size, richness);
    }

    private AutoplaceSettings ReadSingleAutoplaceSettings()
    {
        var treatMissingAsDefault = ReadBoolean();
        var settings = ReadAutoplaceControls();
        return new AutoplaceSettings(treatMissingAsDefault, new(settings));
    }
}
