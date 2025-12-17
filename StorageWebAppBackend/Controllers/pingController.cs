using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StorageWebAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class pingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { success = true, message = "Lambda is alive!" });
        }
    }
}
