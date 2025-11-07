using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class CompetenciesController : ControllerBase
{
    private readonly ICompetencyService _competencyService;
    private readonly IClusterService _clusterService;
    private readonly ILogger<CompetenciesController> _logger;

    public CompetenciesController(
        ICompetencyService competencyService,
        IClusterService clusterService,
        ILogger<CompetenciesController> logger)
    {
        _competencyService = competencyService;
        _clusterService = clusterService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all competencies for the current tenant, optionally filtered by cluster
    /// </summary>
    /// <param name="clusterId">Optional cluster ID to filter by</param>
    /// <returns>List of competencies</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<CompetencyListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CompetencyListResponse>>> GetCompetencies([FromQuery] Guid? clusterId = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var competencies = await _competencyService.GetCompetenciesAsync(tenantId, clusterId);
            return Ok(competencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving competencies");
            return StatusCode(500, new { error = "An error occurred while retrieving competencies" });
        }
    }

    /// <summary>
    /// Get a specific competency by ID
    /// </summary>
    /// <param name="id">Competency ID</param>
    /// <returns>Competency details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CompetencyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompetencyResponse>> GetCompetencyById(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var competency = await _competencyService.GetCompetencyByIdAsync(id);
            
            if (competency == null)
            {
                return NotFound(new { error = $"Competency with ID {id} not found" });
            }

            // Verify the competency belongs to the current tenant
            if (competency.TenantId != tenantId)
            {
                return NotFound(new { error = $"Competency with ID {id} not found" });
            }

            return Ok(competency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving competency {CompetencyId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the competency" });
        }
    }

    /// <summary>
    /// Create a new competency
    /// </summary>
    /// <param name="request">Competency creation request</param>
    /// <returns>Created competency</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CompetencyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompetencyResponse>> CreateCompetency([FromBody] CreateCompetencyRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify cluster exists and belongs to tenant
            var clusterExists = await _clusterService.ClusterExistsAsync(request.ClusterId, tenantId);
            if (!clusterExists)
            {
                return BadRequest(new { error = $"Cluster with ID {request.ClusterId} not found or does not belong to your tenant" });
            }

            var competency = await _competencyService.CreateCompetencyAsync(request, tenantId.Value);

            // Return 201 Created with the created competency
            // Note: CreatedAtAction fails with tenant-scoped routes, so we return Created with a simple status
            return StatusCode(StatusCodes.Status201Created, competency);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating competency");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating competency");
            return StatusCode(500, new { error = "An error occurred while creating the competency" });
        }
    }

    /// <summary>
    /// Update an existing competency
    /// </summary>
    /// <param name="id">Competency ID</param>
    /// <param name="request">Competency update request</param>
    /// <returns>Updated competency</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CompetencyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompetencyResponse>> UpdateCompetency(Guid id, [FromBody] UpdateCompetencyRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify competency exists and belongs to tenant
            var existingCompetency = await _competencyService.GetCompetencyByIdAsync(id);
            if (existingCompetency == null || existingCompetency.TenantId != tenantId)
            {
                return NotFound(new { error = $"Competency with ID {id} not found" });
            }

            // Verify cluster exists and belongs to tenant
            var clusterExists = await _clusterService.ClusterExistsAsync(request.ClusterId, tenantId);
            if (!clusterExists)
            {
                return BadRequest(new { error = $"Cluster with ID {request.ClusterId} not found or does not belong to your tenant" });
            }

            var competency = await _competencyService.UpdateCompetencyAsync(id, request);
            
            if (competency == null)
            {
                return NotFound(new { error = $"Competency with ID {id} not found" });
            }

            return Ok(competency);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating competency {CompetencyId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating competency {CompetencyId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the competency" });
        }
    }

    /// <summary>
    /// Delete a competency (soft delete)
    /// </summary>
    /// <param name="id">Competency ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCompetency(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify competency exists and belongs to tenant
            var existingCompetency = await _competencyService.GetCompetencyByIdAsync(id);
            if (existingCompetency == null || existingCompetency.TenantId != tenantId)
            {
                return NotFound(new { error = $"Competency with ID {id} not found" });
            }

            var result = await _competencyService.DeleteCompetencyAsync(id);
            
            if (!result)
            {
                return NotFound(new { error = $"Competency with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting competency {CompetencyId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the competency" });
        }
    }
}

