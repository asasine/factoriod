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
    [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
    public string IPAddress { get; init; } = null!;

    /// <summary>
    /// The port of the RCON server.
    /// </summary>
    [Range(0, 65535)]
    public ushort Port { get; init; }
}
