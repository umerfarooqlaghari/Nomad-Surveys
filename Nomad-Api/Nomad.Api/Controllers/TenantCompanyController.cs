using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/company")]
[AuthorizeTenant]
public class TenantCompanyController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantCompanyController> _logger;

    public TenantCompanyController(ITenantService tenantService, ILogger<TenantCompanyController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get company information for the current tenant
    /// </summary>
    [HttpGet]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<CompanyResponse>> GetCompany()
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context not found" });
            }

            var company = await _tenantService.GetCompanyByTenantIdAsync(tenantId.Value);
            if (company == null)
            {
                return NotFound(new { message = "Company information not found" });
            }

            return Ok(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company for tenant");
            return StatusCode(500, new { message = "An error occurred while retrieving company information" });
        }
    }

    /// <summary>
    /// Update company information for the current tenant (TenantAdmin only)
    /// </summary>
    [HttpPut]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult> UpdateCompany([FromBody] CreateCompanyRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context not found" });
            }

            var result = await _tenantService.UpdateCompanyAsync(tenantId.Value, request);
            if (!result)
            {
                return NotFound(new { message = "Company not found" });
            }

            return Ok(new { message = "Company information updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company for tenant");
            return StatusCode(500, new { message = "An error occurred while updating company information" });
        }
    }
}
