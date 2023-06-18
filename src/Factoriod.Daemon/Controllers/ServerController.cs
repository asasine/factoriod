using System.Text.Json;
using System.Text.Json.Serialization;
using Factoriod.Daemon.Models;
using Factoriod.Models.Game;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Yoh.Text.Json.NamingPolicies;

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
        var serverSettingsWithSecrets = await this.options.Value.Configuration.GetServerSettingsWithSecretsAsync();
        if (serverSettingsWithSecrets == null)
        {
            this.logger.LogDebug("Server settings file {path} does not exist.", this.options.Value.Configuration.GetServerSettingsPath().FullName);
            return Ok(new ServerSettings());
        }

        // create a ServerSettings with no mutations to drop all inherited properties in ServerSettingsWithSecrets
        // a simple cast is insufficient
        ServerSettings ss = ((ServerSettings)serverSettingsWithSecrets) with { };
        return Ok(ss);
    }

    [HttpPost("settings", Name = "UpdateServerSettings")]
    public async Task<ActionResult<ServerSettingsWithSecrets>> UpdateServerSettings([FromBody] ServerSettingsWithSecrets serverSettingsWithSecrets, [FromQuery] bool restart = false)
    {
        var serverSettingsPath = this.options.Value.Configuration.GetServerSettingsPath();
        using var serverSettingsStream = serverSettingsPath.Exists ? serverSettingsPath.Open(FileMode.Truncate) : serverSettingsPath.OpenWrite();
        this.logger.LogTrace("Writing server settings to file {path}", serverSettingsPath.FullName);
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DictionaryKeyPolicy = JsonNamingPolicies.KebabCaseLower,
            PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower,
            NumberHandling = JsonNumberHandling.Strict,
        };

        await JsonSerializer.SerializeAsync(serverSettingsStream, serverSettingsWithSecrets, jsonOptions);
        serverSettingsStream.WriteByte((byte)'\n');
        await serverSettingsStream.FlushAsync();

        if (restart)
        {
            this.logger.LogInformation("Restarting factorio after writing server settings.");
            await this.factorioProcess.RestartAsync(CancellationToken.None);
        }

        return Ok(serverSettingsWithSecrets);
    }
}
