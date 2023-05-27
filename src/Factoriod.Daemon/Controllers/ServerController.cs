using Factoriod.Daemon.Models;
using Microsoft.AspNetCore.Mvc;

namespace Factoriod.Daemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServerController : ControllerBase
{
    private readonly FactorioProcess factorioProcess;

    public ServerController(FactorioProcess factorioProcess)
    {
        this.factorioProcess = factorioProcess;
    }

    [HttpGet("status", Name = "GetServerStatus")]
    public ServerStatus GetServerStatus() => this.factorioProcess.ServerStatus;
}
