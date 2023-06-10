using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Factoriod.Utilities;

namespace Factoriod.Rcon;

/// <summary>
/// Represents options to configure a <see cref="RconClient"/>.
/// </summary>
public record RconOptions
{
    /// <summary>
    /// The IP address of the RCON server.
    /// </summary>
    [TypeConverter(typeof(IPAddressTypeConverter))]
    public IPAddress IPAddress { get; init; } = null!;

    /// <summary>
    /// The port of the RCON server.
    /// </summary>
    [Range(0, 65535)]
    public ushort Port { get; init; }
}
