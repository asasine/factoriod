using System.ComponentModel.DataAnnotations;
using Factoriod.Utilities;

namespace Factoriod.Models.Game;

/// <summary>
/// The server-settings.json file is the primary settings file for the server.
/// </summary>
/// <param name="Name">Name of the game as it will appear in the game listing.</param>
/// <param name="Description">Description of the game that will appear in the listing.</param>
/// <param name="Tags">Tags of the game that will appear in the listing.</param>
/// <param name="MaxPlayers">Maximum number of players allkowed, admins can join even a full server. Defaults to unlimited.</param>
/// <param name="Visibility">Visibility of the game.</param>
/// <param name="Username">Your factorio.com login credentials. Required for games with <see cref="Visibility.Public"/></param>
/// <param name="RequireUserVerification">When set to true, the server will only allow clients that have a valid Factorio.com account</param>
/// <param name="MaxUploadInKilobytesPerSecond">Maximum number of KB/s that the server will upload. Defaults to unlimited.</param>
/// <param name="MaxUploadSlots">Maximum number of slots the server will upload./param>
/// <param name="MinimumLatencyInTicks">Smallest amount of latency the game can provide. One tick is 16ms.</param>
/// <param name="MaxHeartbeatsPerSecond">Network tick rate. Maximum rate game updates packets are sent at before bundling them together.</param>
/// <param name="IgnorePlayerLimitForReturningPlayers">Players that played on this map already can join even when the max player limit was reached.</param>
/// <param name="AllowCommands">Whether commands are allowed. Possible value are true, false, and admins-only.</param>
/// <param name="AutosaveInterval">Autosave interval in minutes.</param>
/// <param name="AutosaveSlots">Server autosave slots, it is cycled through when the server autosaves.</param>
/// <param name="AfkAutokickInterval">How many minutes until someone is kicked when doing nothing.</param>
/// <param name="AutoPause">Whether should the server be paused when no players are present.</param>
/// <param name="OnlyAdminsCanPauseTheGame">Whether anyone or just admins can pause the game.</param>
/// <param name="AutosaveOnlyOnServer">Whether autosaves should be saved only on server or also on all connected clients.</param>
/// <param name="NonBlockingSaving">Highly experimental feature, enable only at your own risk of losing your saves. True for the server to fork itself to create an autosave. Autosaving on connected Windows clients will be disabled regardless of <paramref name="AutosaveOnlyOnServer"/> option.</param>
/// <param name="MinimumSegmentSize">Long network messages are split into segments that are sent over multiple ticks. Their size depends on the number of peers currently connected. Increasing the segment size will increase upload bandwidth requirement for the server and download bandwidth requirement for clients. This setting only affects server outbound messages. Changing these settings can have a negative impact on connection stability for some clients.</param>
/// <param name="MinimumSegmentSizePeerCount">Long network messages are split into segments that are sent over multiple ticks. Their size depends on the number of peers currently connected. Increasing the segment size will increase upload bandwidth requirement for the server and download bandwidth requirement for clients. This setting only affects server outbound messages. Changing these settings can have a negative impact on connection stability for some clients.</param>
/// <param name="MaximumSegmentSize">Long network messages are split into segments that are sent over multiple ticks. Their size depends on the number of peers currently connected. Increasing the segment size will increase upload bandwidth requirement for the server and download bandwidth requirement for clients. This setting only affects server outbound messages. Changing these settings can have a negative impact on connection stability for some clients.</param>
/// <param name="MaximumSegmentSizePeerCount">Long network messages are split into segments that are sent over multiple ticks. Their size depends on the number of peers currently connected. Increasing the segment size will increase upload bandwidth requirement for the server and download bandwidth requirement for clients. This setting only affects server outbound messages. Changing these settings can have a negative impact on connection stability for some clients.</param>
/// <returns></returns>
public record ServerSettings(
    string Name = "factoriod",
    string Description = "factoriod",
    IReadOnlyCollection<string>? Tags = null,
    uint? MaxPlayers = null,
    Visibility? Visibility = null,
    string Username = "",
    bool RequireUserVerification = true,
    uint? MaxUploadInKilobytesPerSecond = null,
    uint? MaxUploadSlots = null,
    uint? MinimumLatencyInTicks = null,

    [Range(6, 240)]
    uint MaxHeartbeatsPerSecond = 60,
    bool IgnorePlayerLimitForReturningPlayers = false,

    [RegularExpression(@"^((true)|(false)|(admins-only))", ErrorMessage = "Possible values are true, false, and admins-only")]
    string AllowCommands = "admins-only",
    uint AutosaveInterval = 10,
    uint AutosaveSlots = 5,
    uint? AfkAutokickInterval = null,
    bool AutoPause = true,
    bool OnlyAdminsCanPauseTheGame = true,
    bool AutosaveOnlyOnServer = true,
    bool NonBlockingSaving = false,
    uint MinimumSegmentSize = 25,
    uint MinimumSegmentSizePeerCount = 20,
    uint MaximumSegmentSize = 100,
    uint MaximumSegmentSizePeerCount = 10
)
{
    public IReadOnlyCollection<string> Tags { get; } = new PrintableReadOnlyCollection<string>(Tags ?? Array.Empty<string>());
    public uint? MaxPlayers { get; } = MaxPlayers ?? 0;
    public Visibility Visibility { get; } = Visibility ?? new();
    public uint? MaxUploadInKilobytesPerSecond { get; } = MaxUploadInKilobytesPerSecond ?? 0;
    public uint? MaxUploadSlots { get; } = MaxUploadSlots ?? 5;
    public uint? MinimumLatencyInTicks { get; } = MinimumLatencyInTicks ?? 0;
    public uint? AfkAutokickInterval { get; } = AfkAutokickInterval ?? 0;
}

/// <summary>
/// Factorio server settings, including secrets.
/// </summary>
/// <param name="Password">Your factorio.com login credentials. Required for games with <see cref="Visibility.Public"/></param>
/// <param name="Token">Authentication token. May be used instead of <paramref name="Password"/> above.</param>
/// <param name="GamePassword">The password to join the game.</param>
public record ServerSettingsWithSecrets(
    string Password = "",
    string Token = "",
    string GamePassword = ""
) : ServerSettings;

/// <summary>
/// Visibility of a game.
/// </summary>
/// <param name="Public">Game will be published on the official Factorio matching server</param>
/// <param name="Lan">Game will be broadcast on LAN</param>
public record Visibility(
    bool Public = true,
    bool Lan = true
);
