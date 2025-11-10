using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Fap.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint for Docker/Kubernetes
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "FAP Backend API",
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    /// <summary>
    /// Detailed health check with database connection test
    /// </summary>
    [HttpGet("detailed")]
    public IActionResult Detailed([FromServices] Infrastructure.Data.FapDbContext dbContext)
    {
        try
        {
            // Test database connection
            var canConnect = dbContext.Database.CanConnect();

            return Ok(new
            {
                status = canConnect ? "Healthy" : "Degraded",
                timestamp = DateTime.UtcNow,
                service = "FAP Backend API",
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                database = new
                {
                    connected = canConnect,
                    provider = "SQL Server"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}
