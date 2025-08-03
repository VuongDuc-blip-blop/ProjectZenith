using Microsoft.AspNetCore.Mvc;

namespace ProjectZenith.Api.Write.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Checks the health of the API.
        /// </summary>
        /// <returns>A simple OK response.</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Status = "Healthy" });
        }
    }
}
