using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(NomadSurveysDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            // Test database connectivity
            await _context.Database.CanConnectAsync();
            
            var response = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Database = "Connected",
                Version = "1.0.0"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            var response = new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Database = "Disconnected",
                Error = ex.Message,
                Version = "1.0.0"
            };

            return StatusCode(503, response);
        }
    }

    [HttpGet("database")]
    public async Task<ActionResult> GetDatabaseHealth()
    {
        try
        {
            // Test database connectivity and get some basic info
            var canConnect = await _context.Database.CanConnectAsync();
            var surveyCount = await _context.Surveys.CountAsync();
            
            var response = new
            {
                Status = "Connected",
                CanConnect = canConnect,
                SurveyCount = surveyCount,
                ConnectionString = _context.Database.GetConnectionString()?.Substring(0, 50) + "...", // Partial for security
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            
            var response = new
            {
                Status = "Error",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(503, response);
        }
    }
}
