using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Factoriod.Rcon;

/// <summary>
/// Represents options to configure a <see cref="RconClient"/>.
/// </summary>
public record RconOptions
{
    /// <summary>
    /// The IP address of the RCON server.
    /// </summary>
    public IPAddress IPAddress { get; init; }

    /// <summary>
    /// The port of the RCON server.
    /// </summary>
    [Range(0, 65535)]
    public ushort Port { get; init; }
}
