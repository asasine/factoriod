using System.Text.Json;
using Factoriod.Daemon.Models;
using Factoriod.Models.Game;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Factoriod.Daemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServerController : ControllerBase
{
    private readonly FactorioProcess factorioProcess;
    private readonly IOptions<Options.Factorio> options;
    private readonly ILogger<ServerController> logger;

    public ServerController(FactorioProcess factorioProcess, IOptions<Options.Factorio> options, ILogger<ServerController> logger)
    {
        this.factorioProcess = factorioProcess;
        this.options = options;
        this.logger = logger;
    }

    [HttpGet("status", Name = "GetServerStatus")]
    public ServerStatus GetServerStatus() => this.factorioProcess.ServerStatus;

    [HttpGet("settings", Name = "GetServerSettings")]
    public async Task<ActionResult<ServerSettings>> GetServerSettings()
    {
        var serverSettingsPath = this.options.Value.Configuration.GetServerSettingsPath();
        if (!serverSettingsPath.Exists)
        {
            this.logger.LogDebug("Server settings file {path} does not exist.", serverSettingsPath.FullName);
            return Ok(new ServerSettings());
        }

        this.logger.LogTrace("Reading server settings file {path}.", serverSettingsPath.FullName);
        using var serverSettingsStream = serverSettingsPath.OpenRead();
        var serverSettings = await JsonSerializer.DeserializeAsync<ServerSettings>(serverSettingsStream) ?? throw new InvalidOperationException($"Failed to deserialize {serverSettingsPath}");
        return Ok(serverSettings);
    }

    [HttpPost("settings", Name = "UpdateServerSettings")]
    public async Task<ActionResult<ServerSettingsWithSecrets>> UpdateServerSettings([FromBody] ServerSettingsWithSecrets serverSettingsWithSecrets)
    {
        var serverSettingsPath = this.options.Value.Configuration.GetServerSettingsPath();
        if (serverSettingsPath.Exists)
        {
            this.logger.LogDebug("Server settings file {path} exists", serverSettingsPath.FullName);
        }

        this.logger.LogTrace("Writing server settings to file {path}", serverSettingsPath.FullName);
        using var serverSettingsStream = serverSettingsPath.OpenWrite();
        await JsonSerializer.SerializeAsync(serverSettingsStream, serverSettingsWithSecrets);
        return Ok(serverSettingsWithSecrets);
    }
}
