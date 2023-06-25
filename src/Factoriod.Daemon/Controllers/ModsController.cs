using Factoriod.Daemon.Options;
using Factoriod.Fetcher;
using Factoriod.Models.Game;
using Factoriod.Models.Mods;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Factoriod.Daemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ModsController : ControllerBase
{
    private readonly IOptions<Factorio> factorioOptions;
    private readonly ILogger logger;
    private readonly ModFetcher modFetcher;

    public ModsController(IOptions<Factorio> factorioOptions, ILogger<ModsController> logger, ModFetcher modFetcher)
    {
        this.factorioOptions = factorioOptions;
        this.logger = logger;
        this.modFetcher = modFetcher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<string>>> ListModsAsync()
    {
        var modListJson = this.factorioOptions.Value.Configuration.GetModListPath();
        this.logger.LogTrace("Listing mods in {file}", modListJson.FullName);
        if (!modListJson.Exists)
        {
            return Ok(Enumerable.Empty<string>());
        }

        var mods = await ModList.DeserializeFromAsync(modListJson) ?? throw new Exception("Unable to read mod list file.");
        var enabledModNames = mods
            .Mods
            .Where(mod => mod.Enabled)
            .Where(mod => mod.Name != "base")
            .Select(mod => mod.Name);

        return Ok(enabledModNames);
    }

    [HttpPost]
    public async Task<ActionResult> AddModAsync([FromBody] ModListMod mod, [FromQuery] FactorioAuthentication? authentication = null)
    {
        this.logger.LogTrace("Adding {mod}", mod);
        if (!mod.Enabled)
        {
            return BadRequest("Use DELETE to disable mods.");
        }

        if (authentication == null)
        {
            this.logger.LogDebug("Reading authentication credentials from configuration.");

            var serverSettingsWithSecrets = await this.factorioOptions.Value.Configuration.GetServerSettingsAsync<ServerSettingsWithSecrets>()
                ?? throw new InvalidOperationException("Cannot fetch mods: failed to deserialize server settings.");

            authentication = serverSettingsWithSecrets.Authentication;
        }

        if (authentication == null)
        {
            return BadRequest("Credentials must be provided in request query or server settings.");
        }

        var success = await this.modFetcher.DownloadLatestAsync(new Mod(mod.Name), this.factorioOptions.Value.Configuration.GetModListPath(), this.factorioOptions.Value.GetModsRootDirectory(), authentication);
        return success ? Ok() : throw new Exception($"Failed to download mod {mod.Name}");
    }

    [HttpDelete("{name}")]
    public async Task DeleteModAsync([FromRoute] string name)
    {
        this.logger.LogTrace("Deleting {mod}", name);
        var modListPath = this.factorioOptions.Value.Configuration.GetModListPath();
        var mods = await ModList.DeserializeFromAsync(modListPath) ?? throw new Exception("Unable to read mod list file.");

        // disable the mod by setting it to disabled in the mod list and by deleting the zip from the mods cache directory if it exists
        mods = mods.WithDisabled(name);
        await mods.SerializeToAsync(modListPath);

        var modsCacheDirectory = this.factorioOptions.Value.GetModsRootDirectory();
        foreach (var modFsi in modsCacheDirectory.EnumerateFileSystemInfos($"{name}_*.zip"))
        {
            this.logger.LogTrace("Found downloaded mod {path}, deleting.", modFsi.FullName);
            modFsi.Delete();
        }
    }
}
