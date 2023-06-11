using System.Text.Json;
using Factoriod.Daemon.Options;
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

    public ModsController(IOptions<Factorio> factorioOptions, ILogger<ModsController> logger)
    {
        this.factorioOptions = factorioOptions;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<string>>> ListModsAsync()
    {
        var modListJson = this.factorioOptions.Value.GetModListJsonFile();
        this.logger.LogTrace("Listing mods in {file}", modListJson.FullName);
        if (!modListJson.Exists)
        {
            return Ok(Enumerable.Empty<string>());
        }

        using var modListJsonFileStream = modListJson.OpenRead();
        var mods = await JsonSerializer.DeserializeAsync<ModList>(modListJsonFileStream) ?? throw new Exception("Unable to read mod list file.");

        var enabledModNames = mods
            .Mods
            .Where(mod => mod.Enabled)
            .Select(mod => mod.Name);

        return Ok(enabledModNames);
    }
}
