using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to verify API is working
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            message = "Multi-tenant RBAC API is running successfully"
        });
    }

    /// <summary>
    /// Test tenant resolution middleware
    /// </summary>
    [HttpGet("tenant-info")]
    public ActionResult<object> GetTenantInfo()
    {
        var tenantId = HttpContext.Items["TenantId"] as Guid?;
        var tenantSlug = HttpContext.Items["TenantSlug"] as string;

        return Ok(new
        {
            tenantId = tenantId?.ToString() ?? "No tenant resolved",
            tenantSlug = tenantSlug ?? "No tenant slug",
            path = HttpContext.Request.Path.Value,
            message = tenantId.HasValue ? "Tenant successfully resolved" : "No tenant in path"
        });
    }

    /// <summary>
    /// Test authentication (requires valid JWT)
    /// </summary>
    [HttpGet("auth-test")]
    [AuthorizeTenant]
    public ActionResult<object> AuthTest()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var userRoles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var tenantId = User.FindFirst("TenantId")?.Value;

        return Ok(new
        {
            userId,
            userEmail,
            userRoles,
            tenantId,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            message = "Authentication test successful"
        });
    }

    /// <summary>
    /// Test SuperAdmin authorization
    /// </summary>
    [HttpGet("superadmin-test")]
    [AuthorizeSuperAdmin]
    public ActionResult<object> SuperAdminTest()
    {
        return Ok(new
        {
            message = "SuperAdmin access granted",
            timestamp = DateTime.UtcNow,
            user = User.Identity?.Name
        });
    }

    /// <summary>
    /// Test TenantAdmin authorization
    /// </summary>
    [HttpGet("tenantadmin-test")]
    [AuthorizeTenantAdmin]
    public ActionResult<object> TenantAdminTest()
    {
        return Ok(new
        {
            message = "TenantAdmin access granted",
            timestamp = DateTime.UtcNow,
            user = User.Identity?.Name,
            tenantId = HttpContext.Items["TenantId"]
        });
    }
}
