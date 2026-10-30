using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace RentalManager.Api.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController(IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("info")]
    public ActionResult<SystemInfoResponse> GetInfo()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

        return Ok(new SystemInfoResponse("RentalManager.Api", version, environment.EnvironmentName, DateTime.UtcNow));
    }
}

public record SystemInfoResponse(string Service, string Version, string Environment, DateTime UtcNow);
