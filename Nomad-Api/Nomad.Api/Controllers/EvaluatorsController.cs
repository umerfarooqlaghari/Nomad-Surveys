using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class EvaluatorsController : ControllerBase
{
    private readonly IEvaluatorService _evaluatorService;
    private readonly ILogger<EvaluatorsController> _logger;

    public EvaluatorsController(IEvaluatorService evaluatorService, ILogger<EvaluatorsController> logger)
    {
        _evaluatorService = evaluatorService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all evaluators with optional tenant filtering
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter. If null, shows all evaluators (SuperAdmin only)</param>
    /// <returns>List of evaluators</returns>
    [HttpGet]
    public async Task<ActionResult<List<EvaluatorListResponse>>> GetEvaluators([FromQuery] Guid? tenantId = null)
    {
        try
        {
            var currentTenantId = GetCurrentTenantId();
            
            // If user is not SuperAdmin, they can only see their own tenant's evaluators
            if (currentTenantId.HasValue && tenantId.HasValue && tenantId != currentTenantId)
            {
                return Forbid("You can only access evaluators from your own tenant");
            }

            // Use current tenant if no specific tenant requested and user is not SuperAdmin
            var filterTenantId = tenantId ?? currentTenantId;

            var evaluators = await _evaluatorService.GetEvaluatorsAsync(filterTenantId);
            
            _logger.LogInformation("Retrieved {Count} evaluators for tenant {TenantId}", evaluators.Count, filterTenantId);
            
            return Ok(evaluators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evaluators");
            return StatusCode(500, new { message = "An error occurred while retrieving evaluators" });
        }
    }

    /// <summary>
    /// Get a specific evaluator by ID
    /// </summary>
    /// <param name="id">Evaluator ID</param>
    /// <returns>Evaluator details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<EvaluatorResponse>> GetEvaluator(Guid id)
    {
        try
        {
            var evaluator = await _evaluatorService.GetEvaluatorByIdAsync(id);
            
            if (evaluator == null)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            
            // Ensure user can only access evaluators from their tenant (unless SuperAdmin)
            if (currentTenantId.HasValue && evaluator.TenantId != currentTenantId)
            {
                return Forbid("You can only access evaluators from your own tenant");
            }

            _logger.LogInformation("Retrieved evaluator {EvaluatorId}", id);
            
            return Ok(evaluator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evaluator {EvaluatorId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the evaluator" });
        }
    }

    /// <summary>
    /// Bulk create evaluators (works for single evaluator as well)
    /// </summary>
    /// <param name="request">Bulk create request with list of evaluators</param>
    /// <returns>Bulk creation result</returns>
    [HttpPost("bulk")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<BulkCreateResponse>> BulkCreateEvaluators([FromBody] BulkCreateEvaluatorsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentTenantId = GetCurrentTenantId();
            if (!currentTenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context is required" });
            }

            var result = await _evaluatorService.BulkCreateEvaluatorsAsync(request, currentTenantId.Value);
            
            _logger.LogInformation("Bulk created {SuccessCount}/{TotalCount} evaluators for tenant {TenantId}", 
                result.SuccessfullyCreated, result.TotalRequested, currentTenantId);

            if (result.SuccessfullyCreated == 0)
            {
                return BadRequest(result);
            }

            if (result.Failed > 0)
            {
                return StatusCode(207, result); // Multi-Status for partial success
            }

            return CreatedAtAction(nameof(GetEvaluators), new { tenantId = currentTenantId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating evaluators");
            return StatusCode(500, new { message = "An error occurred while creating evaluators" });
        }
    }

    /// <summary>
    /// Update an evaluator
    /// </summary>
    /// <param name="id">Evaluator ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated evaluator</returns>
    [HttpPut("{id}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<EvaluatorResponse>> UpdateEvaluator(Guid id, [FromBody] UpdateEvaluatorRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if evaluator exists and user has access
            var existingEvaluator = await _evaluatorService.GetEvaluatorByIdAsync(id);
            if (existingEvaluator == null)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && existingEvaluator.TenantId != currentTenantId)
            {
                return Forbid("You can only update evaluators from your own tenant");
            }

            var updatedEvaluator = await _evaluatorService.UpdateEvaluatorAsync(id, request);
            
            if (updatedEvaluator == null)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            _logger.LogInformation("Updated evaluator {EvaluatorId}", id);
            
            return Ok(updatedEvaluator);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating evaluator {EvaluatorId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating evaluator {EvaluatorId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the evaluator" });
        }
    }

    /// <summary>
    /// Delete an evaluator (soft delete)
    /// </summary>
    /// <param name="id">Evaluator ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult> DeleteEvaluator(Guid id)
    {
        try
        {
            // Check if evaluator exists and user has access
            var existingEvaluator = await _evaluatorService.GetEvaluatorByIdAsync(id);
            if (existingEvaluator == null)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && existingEvaluator.TenantId != currentTenantId)
            {
                return Forbid("You can only delete evaluators from your own tenant");
            }

            var deleted = await _evaluatorService.DeleteEvaluatorAsync(id);
            
            if (!deleted)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            _logger.LogInformation("Deleted evaluator {EvaluatorId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting evaluator {EvaluatorId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the evaluator" });
        }
    }
}
