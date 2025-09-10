using System.Security.Claims;

namespace Nomad.Api.Middleware;

public class TenantAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantAuthorizationMiddleware> _logger;

    public TenantAuthorizationMiddleware(RequestDelegate next, ILogger<TenantAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authorization for non-authenticated requests
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        try
        {
            var tenantIdFromPath = context.Items["TenantId"] as Guid?;
            var tenantIdFromClaims = GetTenantIdFromClaims(context.User);
            var userRoles = GetRolesFromClaims(context.User);

            // SuperAdmin can access any tenant
            if (userRoles.Contains("SuperAdmin"))
            {
                _logger.LogInformation("SuperAdmin access granted for user {UserId}", context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _next(context);
                return;
            }

            // For tenant-specific endpoints, validate tenant access
            if (tenantIdFromPath.HasValue)
            {
                if (!tenantIdFromClaims.HasValue)
                {
                    _logger.LogWarning("User {UserId} has no tenant claim but accessing tenant-specific endpoint", 
                        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Access denied: No tenant access");
                    return;
                }

                if (tenantIdFromPath.Value != tenantIdFromClaims.Value)
                {
                    _logger.LogWarning("Tenant mismatch for user {UserId}. Path: {PathTenant}, Claims: {ClaimsTenant}", 
                        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                        tenantIdFromPath.Value,
                        tenantIdFromClaims.Value);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Access denied: Tenant mismatch");
                    return;
                }

                _logger.LogInformation("Tenant access validated for user {UserId} on tenant {TenantId}", 
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    tenantIdFromClaims.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant authorization for user {UserId}", 
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error during authorization");
            return;
        }

        await _next(context);
    }

    private static Guid? GetTenantIdFromClaims(ClaimsPrincipal user)
    {
        var tenantIdClaim = user.FindFirst("TenantId")?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    private static List<string> GetRolesFromClaims(ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    }
}
