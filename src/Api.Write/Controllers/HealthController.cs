// src/Api.Write/Controllers/HealthController.cs
using Microsoft.AspNetCore.Mvc;

namespace ProjectZenith.Api.Write.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly ConfigService _configService;

    public HealthController(ConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// Returns a simple health check response.
    /// </summary>
    /// <returns>A string indicating the service is healthy.</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Configuration = _configService.GetConfigSummary()
        });
    }
}