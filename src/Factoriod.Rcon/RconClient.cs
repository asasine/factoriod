using CoreRCON;
using Microsoft.Extensions.Options;

namespace Factoriod.Rcon;

/// <summary>
/// Remotely connects to a factorio game server and sends console commands.
/// </summary>
public sealed class RconClient : IDisposable
{
    /// <summary>
    /// The RCON client options.
    /// </summary>
    private readonly IOptions<RconOptions> rconOptions;

    /// <summary>
    /// The password for the RCON server.
    /// </summary>
    private string? password = null;

    /// <summary>
    /// The current RCON client.
    /// </summary>
    private RCON? rcon = null;

    /// <summary>
    /// Creates an RCON client.
    /// </summary>
    /// <param name="rconOptions">The RCON client options.</param>
    public RconClient(IOptions<RconOptions> rconOptions)
    {
        this.rconOptions = rconOptions;
    }

    /// <summary>
    /// Configures the RCON client with a new password.
    /// The client will reconnect on the next command.
    /// </summary>
    /// <param name="password">The password for the RCON server.</param>
    public void Configure(string password)
    {
        this.password = password;
        Dispose();
    }

    public void Dispose()
    {
        rcon?.Dispose();
        rcon = null;
    }

    /// <summary>
    /// Lists online players.
    /// </summary>
    /// <returns>Online players.</returns>
    public async Task<string> ListOnlinePlayersAsync()
    {
        var rcon = await GetClientAsync();
        return await rcon.SendCommandAsync("/players");
    }

    /// <summary>
    /// Gets a connected client for the RCON server.
    /// </summary>
    /// <returns>A connected client for the RCON server.</returns>
    /// <exception cref="InvalidOperationException">If a password is not configured through <see cref="Configure(string)"/></exception>
    private async Task<RCON> GetClientAsync()
    {
        if (rcon != null)
        {
            return rcon;
        }

        if (password == null)
        {
            throw new InvalidOperationException($"Cannot create an RCON client without a password. Please call {nameof(Configure)} first.");
        }

        rcon = new RCON(rconOptions.Value.IPAddress, rconOptions.Value.Port, password);
        await rcon.ConnectAsync();
        return rcon;
    }
}
