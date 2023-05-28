namespace Factoriod.Models.Game.Test;

public class FactorioBinaryReaderTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(byte.MaxValue, false)]
    public void ReadBoolean(byte value, bool expected)
    {
        var bytes = new byte[] { value };
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadBoolean();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(254)]
    public void ReadUInt16SpaceOptimized(byte expected)
    {
        var bytes = new byte[] { expected };
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadUInt16();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(ushort.MaxValue)]
    public void ReadUInt16(ushort expected)
    {
        // 255 is the magic number that indicates the next two bytes are the value
        // value is stored as little endian
        var bytes = new byte[]
        {
            255,
            (byte)expected,
            (byte)(expected >> 8),
        };

        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadUInt16();

        Assert.Equal(expected, actual);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(254)]
    public void ReadInt16SpaceOptimized(byte expected)
    {
        var bytes = new byte[] { expected };
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadInt16();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(short.MinValue)]
    [InlineData(-1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(short.MaxValue)]
    public void ReadInt16(short expected)
    {
        // 255 is the magic number that indicates the next two bytes are the value
        // value is stored as little endian
        var bytes = new byte[]
        {
            255,
            (byte)expected,
            (byte)(expected >> 8),
        };

        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadInt16();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(254)]
    public void ReadUInt32SpaceOptimized(byte expected)
    {
        var bytes = new byte[] { expected };
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadUInt32();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(uint.MaxValue)]
    public void ReadUInt32(uint expected)
    {
        // 255 is the magic number that indicates the next two bytes are the value
        // value is stored as little endian
        var bytes = new byte[]
        {
            255,
            (byte)expected,
            (byte)(expected >> 8),
            (byte)(expected >> 16),
            (byte)(expected >> 24),
        };

        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadUInt32();

        Assert.Equal(expected, actual);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(254)]
    public void ReadInt32SpaceOptimized(byte expected)
    {
        var bytes = new byte[] { expected };
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadInt32();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(int.MaxValue)]
    public void ReadInt32(int expected)
    {
        // 255 is the magic number that indicates the next two bytes are the value
        // value is stored as little endian
        var bytes = new byte[]
        {
            255,
            (byte)expected,
            (byte)(expected >> 8),
            (byte)(expected >> 16),
            (byte)(expected >> 24),
        };

        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadInt32();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(254)]
    public void ReadUInt64SpaceOptimized(byte expected)
    {
        var bytes = new byte[] { expected };
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadUInt64();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(ulong.MaxValue)]
    public void ReadUInt64(ulong expected)
    {
        // 255 is the magic number that indicates the next two bytes are the value
        // value is stored as little endian
        var bytes = new byte[]
        {
            255,
            (byte)expected,
            (byte)(expected >> 8),
            (byte)(expected >> 16),
            (byte)(expected >> 24),
            (byte)(expected >> 32),
            (byte)(expected >> 40),
            (byte)(expected >> 48),
            (byte)(expected >> 56),
        };

        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadUInt64();

        Assert.Equal(expected, actual);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(254)]
    public void ReadInt64SpaceOptimized(byte expected)
    {
        var bytes = new byte[] { expected };
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadInt64();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(long.MaxValue)]
    public void ReadInt64(long expected)
    {
        // 255 is the magic number that indicates the next two bytes are the value
        // value is stored as little endian
        var bytes = new byte[]
        {
            255,
            (byte)expected,
            (byte)(expected >> 8),
            (byte)(expected >> 16),
            (byte)(expected >> 24),
            (byte)(expected >> 32),
            (byte)(expected >> 40),
            (byte)(expected >> 48),
            (byte)(expected >> 56),
        };

        using var memoryStream = new MemoryStream(bytes);
        using var reader = new FactorioBinaryReader(memoryStream);

        var actual = reader.ReadInt64();

        Assert.Equal(expected, actual);
    }
}
