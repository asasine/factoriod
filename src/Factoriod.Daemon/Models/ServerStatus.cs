using Factoriod.Models;

namespace Factoriod.Daemon.Models;

/// <summary>
/// Models the state of the factorio game server.
/// </summary>
public class ServerStatus
{
    /// <summary>
    /// The current state of the game server.
    /// </summary>
    public ServerState ServerState { get; set; }

    /// <summary>
    /// An exception when <see cref="ServerState"/> is <see cref="ServerState.Faulted"/>.
    /// </summary>
    public FactorioException? Exception { get; set; }

    /// <summary>
    /// The active save.
    /// </summary>
    public Save? Save { get; set; } = null;

    /// <summary>
    /// Sets the game server's status to running with the provided <paramref name="save"/>.
    /// </summary>
    /// <param name="save">The save that the game server is running.</param>
    public void SetRunning(Save save)
    {
        this.Save = save;
        this.ServerState = ServerState.Running;
    }

    /// <summary>
    /// Sets the game server's status to faulted with the provided <paramref name="exception"/>.
    /// </summary>
    /// <param name="exception">The exception that the game server encountered.</param>
    public void SetFaulted(FactorioException exception)
    {
        this.Exception = exception;
        this.ServerState = ServerState.Faulted;
    }
}

/// <summary>
/// Possible states for the factorio game server to be in.
/// </summary>
public enum ServerState
{
    /// <summary>
    /// The server is launching.
    /// </summary>
    Launching,

    /// <summary>
    /// The server is running.
    /// </summary>
    Running,

    /// <summary>
    /// The server has exited.
    /// </summary>
    Exited,

    /// <summary>
    /// The server is in a faulted state.
    /// </summary>
    Faulted,
}