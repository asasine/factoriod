using Factoriod.Rcon;
using Microsoft.AspNetCore.Mvc;

namespace Factoriod.Daemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RconController : ControllerBase
{
    private readonly RconClient rconClient;
    private readonly IHostEnvironment hostEnvironment;

    public RconController(RconClient rconClient, IHostEnvironment hostEnvironment)
    {
        this.rconClient = rconClient;
        this.hostEnvironment = hostEnvironment;
    }

    [HttpGet("players")]
    public async Task<IEnumerable<string>> ListOnlinePlayersAsync()
    {
        var players = rconClient.ListOnlinePlayersAsync();
        return await players.ToListAsync();
    }

    [HttpGet("launches")]
    public async Task<IReadOnlyDictionary<string, int>> GetItemLaunchesAsync() => await rconClient.GetItemsLaunchedAsync();

    [HttpPost("custom")]
    public async Task<ActionResult<string>> SendCustomCommandAsync([FromBody] string command)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await rconClient.SendCustomCommandAsync(command);
        return Ok(result);
    }
}
