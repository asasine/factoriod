using Factoriod.Rcon;
using Microsoft.AspNetCore.Mvc;

namespace Factoriod.Daemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RconController : ControllerBase
{
    private readonly RconClient rconClient;

    public RconController(RconClient rconClient)
    {
        this.rconClient = rconClient;
    }

    [HttpGet]
    public async Task<IEnumerable<string>> ListOnlinePlayersAsync()
    {
        var players = await this.rconClient.ListOnlinePlayersAsync();
        return new string[] { players };
    }
}
