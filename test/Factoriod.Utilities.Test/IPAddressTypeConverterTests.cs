using System.Net;

namespace Factoriod.Utilities.Test;

public class IPAddressTypeConverterTests
{
    private static IEnumerable<IPAddress> IPAddressTestCases()
    {
        yield return IPAddress.Loopback;
        yield return IPAddress.Parse("192.168.0.1");
        yield return IPAddress.Parse("0.0.0.0");
        yield return IPAddress.Parse("255.255.255.255");
    }

    private static IEnumerable<(Type type, Func<IPAddress, object> converter)> SimpleConverters()
    {
        yield return (typeof(IPAddress), ipAddress => ipAddress);
        yield return (typeof(string), ipAddress => ipAddress.ToString());
        yield return (typeof(byte[]), ipAddress => ipAddress.GetAddressBytes());
    }

    public static IEnumerable<object[]> ConvertableTypeTestCases()
        => SimpleConverters().Select(converter => new object[] { converter.type });


    public static IEnumerable<object[]> ConvertFromTestCases()
    {
        foreach (var ipAddress in IPAddressTestCases())
        {
            foreach (var converter in SimpleConverters())
            {
                yield return new object[] { converter.converter(ipAddress), ipAddress };
            }
        }

        yield return new object[] { "localhost", IPAddress.Loopback };
    }

    public static IEnumerable<object[]> ConvertToTestCases()
    {
        foreach (var ipAddress in IPAddressTestCases())
        {
            foreach (var converter in SimpleConverters())
            {
                yield return new object[] { ipAddress, converter.type, converter.converter(ipAddress) };
            }
        }
    }

    [Theory]
    [MemberData(nameof(ConvertableTypeTestCases))]
    public void CanConvertFromType(Type type)
    {
        var converter = new IPAddressTypeConverter();
        Assert.True(converter.CanConvertFrom(type));
    }

    [Fact]
    public void CannotConvertFromArbitraryTypes()
    {
        var converter = new IPAddressTypeConverter();
        Assert.False(converter.CanConvertFrom(typeof(object)));

        var anon = new { };
        Assert.False(converter.CanConvertFrom(anon.GetType()));
    }

    [Theory]
    [MemberData(nameof(ConvertFromTestCases))]
    public void ConvertFrom(object input, IPAddress expected)
    {
        var converter = new IPAddressTypeConverter();
        var actual = converter.ConvertFrom(input);
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ConvertableTypeTestCases))]
    public void CanConvertToType(Type type)
    {
        var converter = new IPAddressTypeConverter();
        Assert.True(converter.CanConvertTo(type));
    }

    [Fact]
    public void CannotConvertToArbitraryTypes()
    {
        var converter = new IPAddressTypeConverter();
        Assert.False(converter.CanConvertTo(typeof(object)));

        var anon = new { };
        Assert.False(converter.CanConvertTo(anon.GetType()));
    }

    [Theory]
    [MemberData(nameof(ConvertToTestCases))]
    public void ConvertTo(IPAddress input, Type destinationType, object expected)
    {
        var converter = new IPAddressTypeConverter();
        var actual = converter.ConvertTo(input, destinationType);
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ConvertableTypeTestCases))]
    public void ConvertToThrowsArgumentNullException(Type destinationType)
    {
        var converter = new IPAddressTypeConverter();
        Assert.ThrowsAny<ArgumentNullException>(() => converter.ConvertTo(null, destinationType));
    }

    [Theory]
    [MemberData(nameof(ConvertableTypeTestCases))]
    public void ConvertToThrowsConvertToExceptionOnInvalidValueType(Type destinationType)
    {
        var converter = new IPAddressTypeConverter();

        int value = 42; // anything that isn't IPAddress
        Assert.ThrowsAny<NotSupportedException>(() => converter.ConvertTo(value, destinationType));
    }
}
