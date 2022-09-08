using Factoriod.Models;
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
        public IEnumerable<Save> List()
        {
            var savesRootDirectory = this.options.Saves.GetRootDirectory();

            this.logger.LogDebug("Scanning {path} for saves", savesRootDirectory);

            // ensure it's created, otherwise a DirectoryNotFoundException is thrown
            savesRootDirectory.Create();

            // choose the save which was modified most recently
            return savesRootDirectory
                .EnumerateFiles()
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => new Save(file.FullName));
        }

        [HttpGet("{name}", Name = "GetSave")]
        public ActionResult<Save> Get(string name)
        {
            var file = Path.Combine(this.options.Saves.RootDirectory, $"{name}.zip");
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
        public ActionResult<Save> SetSave(string name)
        {
            var file = PathUtilities.Resolve(Path.Combine(this.options.Saves.RootDirectory, $"{name}.zip"));
            this.logger.LogDebug("Attempting to set save to {path} for a save named {save}", file, name);
            if (!System.IO.File.Exists(file))
            {
                return NotFound();
            }

            var save = new Save(file);
            this.factorioProcess.SetSave(save);
            return AcceptedAtRoute("GetServerStatus");
        }
    }
}
