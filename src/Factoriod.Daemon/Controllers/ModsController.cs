using Factoriod.Daemon.Options;
using Factoriod.Fetcher;
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

        var mods = await ModList.DeserialzeFromAsync(modListJson) ?? throw new Exception("Unable to read mod list file.");
        var enabledModNames = mods
            .Mods
            .Where(mod => mod.Enabled)
            .Where(mod => mod.Name != "base")
            .Select(mod => mod.Name);

        return Ok(enabledModNames);
    }

    [HttpPost]
    public async Task<ActionResult> AddModAsync([FromBody] ModListMod mod, [FromQuery] FactorioAuthentication authentication)
    {
        this.logger.LogTrace("Adding {mod}", mod);
        if (!mod.Enabled)
        {
            return BadRequest("Use DELETE to disable mods.");
        }

        var success = await this.modFetcher.DownloadLatestAsync(new Mod(mod.Name), this.factorioOptions.Value.Configuration.GetModListPath(), this.factorioOptions.Value.GetModsRootDirectory(), authentication);
        return success ? Ok() : throw new Exception($"Failed to download mod {mod.Name}");
    }
}
