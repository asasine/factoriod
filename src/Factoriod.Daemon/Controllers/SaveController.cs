﻿using Factoriod.Models;
using Factoriod.Models.Game;
using Factoriod.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Factoriod.Daemon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaveController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly Options.Factorio options;
        private readonly FactorioProcess factorioProcess;

        public SaveController(ILogger<SaveController> logger, IOptions<Options.Factorio> options, FactorioProcess factorioProcess)
        {
            this.logger = logger;
            this.options = options.Value;
            this.factorioProcess = factorioProcess;
        }

        [HttpGet(Name = "ListSaves")]
        public IEnumerable<Save> List([FromQuery] bool backups = false)
            => this.options.Saves.ListSaves()
                .Where(save => save.IsBackup == backups);


        [HttpGet("{name}", Name = "GetSave")]
        public ActionResult<Save> Get(string name)
        {
            var file = PathUtilities.Resolve(Path.Combine(this.options.Saves.RootDirectory, $"{name}.zip"));
            this.logger.LogDebug("Scanning {path} for a save named {save}", file, name);
            if (System.IO.File.Exists(file))
            {
                return Ok(new Save(file));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPut("{name}", Name = "SetSave")]
        public async Task<ActionResult<Save>> SetSaveAsync(string name)
        {
            var file = PathUtilities.Resolve(Path.Combine(this.options.Saves.RootDirectory, $"{name}.zip"));
            this.logger.LogDebug("Attempting to set save to {path} for a save named {save}", file, name);
            if (!System.IO.File.Exists(file))
            {
                this.logger.LogDebug("Save {save} not found", name);
                return NotFound();
            }

            var save = new Save(file);
            await this.factorioProcess.SetSave(save);
            return AcceptedAtRoute("GetServerStatus");
        }

        [HttpPut("create/{name}", Name = "CreateSave")]
        public async Task<ActionResult<MapExchangeStringData>> CreateSave(string name, [FromBody] MapExchangeStringData mapExchangeStringData, [FromQuery] bool overwrite = false)
        {
            this.logger.LogDebug("Attempting to create save named {save}", name);
            var success = await this.factorioProcess.CreateSaveAsync(name, mapExchangeStringData, overwrite: overwrite);
            if (!success)
            {
                return BadRequest();
            }

            return AcceptedAtRoute("GetServerStatus", null, mapExchangeStringData);
        }
    }
}
