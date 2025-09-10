using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.Entities;

namespace Nomad.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, NomadSurveysDbContext dbContext)
    {
        try
        {
            var tenantSlug = ExtractTenantSlug(context.Request.Path);
            
            if (!string.IsNullOrEmpty(tenantSlug))
            {
                // Temporarily disable query filters to find tenant
                using var scope = dbContext.Database.BeginTransaction();
                
                var tenant = await dbContext.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive);
                
                if (tenant != null)
                {
                    context.Items["TenantId"] = tenant.Id;
                    context.Items["TenantSlug"] = tenant.Slug;
                    context.Items["Tenant"] = tenant;
                    
                    _logger.LogInformation("Tenant resolved: {TenantSlug} -> {TenantId}", tenantSlug, tenant.Id);
                }
                else
                {
                    _logger.LogWarning("Tenant not found or inactive: {TenantSlug}", tenantSlug);
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"Tenant '{tenantSlug}' not found or inactive");
                    return;
                }
                
                scope.Rollback(); // We only needed to read, rollback the transaction
            }
            else
            {
                // For non-tenant specific endpoints (like super admin endpoints)
                _logger.LogInformation("No tenant slug found in path: {Path}", context.Request.Path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant from path: {Path}", context.Request.Path);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error during tenant resolution");
            return;
        }

        await _next(context);
    }

    private static string? ExtractTenantSlug(PathString path)
    {
        // Extract tenant slug from path like: /{tenantSlug}/api/surveys
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (segments?.Length >= 1)
        {
            var firstSegment = segments[0];
            
            // Skip common non-tenant paths
            if (IsNonTenantPath(firstSegment))
            {
                return null;
            }
            
            return firstSegment;
        }
        
        return null;
    }

    private static bool IsNonTenantPath(string segment)
    {
        var nonTenantPaths = new[]
        {
            "api", "swagger", "health", "admin", "auth", "login", "register", 
            "favicon.ico", "robots.txt", "_framework", "css", "js", "images"
        };
        
        return nonTenantPaths.Contains(segment.ToLowerInvariant());
    }
}
