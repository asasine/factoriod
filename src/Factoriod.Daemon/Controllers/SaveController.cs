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

        public SaveController(ILogger<SaveController> logger, IOptions<Options.Factorio> options)
        {
            this.logger = logger;
            this.options = options.Value;
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
                .Select(file => new Save(file));
        }

        [HttpGet("{name}", Name = "GetSave")]
        public ActionResult<Save> Get(string name)
        {
            var file = new FileInfo(Path.Combine(this.options.Saves.RootDirectory, $"{name}.zip")).Resolve();
            this.logger.LogDebug("Scanning {path} for a save named {save}", file, name);
            if (file.Exists)
            {
                return Ok(new Save(file));
            }
            else
            {
                return NotFound();
            }
        }
    }
}
