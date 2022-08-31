using Microsoft.AspNetCore.Mvc;

namespace Factoriod.Daemon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaveController : ControllerBase
    {
        [HttpGet(Name = "ListSaves")]
        public IAsyncEnumerable<string> List()
        {
            return Enumerable.Range(1, 5)
                .Select(x => x.ToString())
                .ToAsyncEnumerable();
        }
    }
}
