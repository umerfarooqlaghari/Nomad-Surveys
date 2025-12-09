using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/settings")]
[AuthorizeTenant]
public class TenantSettingsController : ControllerBase
{
    private readonly ITenantSettingsService _tenantSettingsService;
    private readonly ILogger<TenantSettingsController> _logger;

    public TenantSettingsController(
        ITenantSettingsService tenantSettingsService,
        ILogger<TenantSettingsController> logger)
    {
        _tenantSettingsService = tenantSettingsService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get tenant settings
    /// </summary>
    /// <returns>Tenant settings</returns>
    [HttpGet]
    [ProducesResponseType(typeof(TenantSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantSettingsResponse>> GetSettings()
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var settings = await _tenantSettingsService.GetSettingsByTenantIdAsync(tenantId.Value);

            if (settings == null)
            {
                return NotFound(new { error = $"Settings not found for tenant" });
            }

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant settings");
            return StatusCode(500, new { error = "An error occurred while retrieving tenant settings" });
        }
    }

    /// <summary>
    /// Create tenant settings
    /// </summary>
    /// <param name="request">Tenant settings data</param>
    /// <returns>Created tenant settings</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TenantSettingsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantSettingsResponse>> CreateSettings([FromBody] CreateTenantSettingsRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var settings = await _tenantSettingsService.CreateSettingsAsync(request, tenantId.Value);

            return StatusCode(StatusCodes.Status201Created, settings);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating tenant settings");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant settings");
            return StatusCode(500, new { error = "An error occurred while creating tenant settings" });
        }
    }

    /// <summary>
    /// Update tenant settings
    /// </summary>
    /// <param name="request">Updated tenant settings data</param>
    /// <returns>Updated tenant settings</returns>
    [HttpPut]
    [ProducesResponseType(typeof(TenantSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantSettingsResponse>> UpdateSettings([FromBody] UpdateTenantSettingsRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var settings = await _tenantSettingsService.UpdateSettingsAsync(request, tenantId.Value);

            return Ok(settings);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating tenant settings");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant settings");
            return StatusCode(500, new { error = "An error occurred while updating tenant settings" });
        }
    }
}

