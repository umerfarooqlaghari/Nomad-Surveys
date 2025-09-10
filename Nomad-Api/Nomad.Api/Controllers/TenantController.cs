using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ITenantService tenantService, ILogger<TenantController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new tenant (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult<TenantResponse>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            var tenant = await _tenantService.CreateTenantAsync(request);
            return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create tenant: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return StatusCode(500, new { message = "An error occurred while creating the tenant" });
        }
    }

    /// <summary>
    /// Get all tenants (SuperAdmin only)
    /// </summary>
    [HttpGet]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult<List<TenantListResponse>>> GetTenants()
    {
        try
        {
            var tenants = await _tenantService.GetTenantsAsync();
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants");
            return StatusCode(500, new { message = "An error occurred while retrieving tenants" });
        }
    }

    /// <summary>
    /// Get tenant by ID (SuperAdmin only)
    /// </summary>
    [HttpGet("{id:guid}")]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult<TenantResponse>> GetTenant(Guid id)
    {
        try
        {
            var tenant = await _tenantService.GetTenantByIdAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the tenant" });
        }
    }

    /// <summary>
    /// Get tenant by slug (SuperAdmin only)
    /// </summary>
    [HttpGet("by-slug/{slug}")]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult<TenantResponse>> GetTenantBySlug(string slug)
    {
        try
        {
            var tenant = await _tenantService.GetTenantBySlugAsync(slug);
            if (tenant == null)
            {
                return NotFound();
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant by slug {TenantSlug}", slug);
            return StatusCode(500, new { message = "An error occurred while retrieving the tenant" });
        }
    }

    /// <summary>
    /// Update tenant (SuperAdmin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult> UpdateTenant(Guid id, [FromBody] CreateTenantRequest request)
    {
        try
        {
            var result = await _tenantService.UpdateTenantAsync(id, request);
            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Tenant updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to update tenant {TenantId}: {Message}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the tenant" });
        }
    }

    /// <summary>
    /// Deactivate tenant (SuperAdmin only)
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult> DeactivateTenant(Guid id)
    {
        try
        {
            var result = await _tenantService.DeactivateTenantAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Tenant deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating tenant {TenantId}", id);
            return StatusCode(500, new { message = "An error occurred while deactivating the tenant" });
        }
    }

    /// <summary>
    /// Activate tenant (SuperAdmin only)
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult> ActivateTenant(Guid id)
    {
        try
        {
            var result = await _tenantService.ActivateTenantAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Tenant activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant {TenantId}", id);
            return StatusCode(500, new { message = "An error occurred while activating the tenant" });
        }
    }
}
