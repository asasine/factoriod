using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace Factoriod.Utilities;

public class IPAddressTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => IsSupportedType(sourceType) || base.CanConvertFrom(context, sourceType);

    public override IPAddress? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value switch
        {
            IPAddress ipAddress => ipAddress,
            string ipString => ParseIPAddress(ipString),
            byte[] ipBytes => new IPAddress(ipBytes),
            _ => base.ConvertFrom(context, culture, value) as IPAddress,
        };

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => IsSupportedType(destinationType) || base.CanConvertTo(context, destinationType);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (value.GetType() == destinationType)
        {
            return value;
        }

        if (value is not IPAddress ipAddress)
        {
            throw GetConvertToException(value, destinationType);
        }

        if (destinationType == typeof(IPAddress))
        {
            return ipAddress;
        }

        if (destinationType == typeof(string))
        {
            return ipAddress.ToString();
        }

        if (destinationType == typeof(byte[]))
        {
            return ipAddress.GetAddressBytes();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    private static bool IsSupportedType(Type? type) => type != null &&
        (type == typeof(IPAddress)
        || type == typeof(string)
        || type == typeof(byte[]));

    private static IPAddress ParseIPAddress(ReadOnlySpan<char> ipAddress)
    {
        if (ipAddress == "localhost")
        {
            return IPAddress.Loopback;
        }

        return IPAddress.Parse(ipAddress);
    }
}
