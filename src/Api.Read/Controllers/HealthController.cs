using Microsoft.AspNetCore.Mvc;

namespace ProjectZenith.Api.Read.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Returns a simple health check response.
        /// </summary>
        /// <returns>A string indicating the service is healthy.</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Status = "Healthy" });
        }
    }
}
