using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;
using Nomad.Api.Services;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class EvaluatorsController : ControllerBase
{
    private readonly IEvaluatorService _evaluatorService;
    private readonly IRelationshipService _relationshipService;
    private readonly ILogger<EvaluatorsController> _logger;

    public EvaluatorsController(IEvaluatorService evaluatorService, IRelationshipService relationshipService, ILogger<EvaluatorsController> logger)
    {
        _evaluatorService = evaluatorService;
        _relationshipService = relationshipService;
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
            _logger.LogInformation("Received bulk create evaluators request with {Count} evaluators", request?.Evaluators?.Count ?? 0);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            var currentTenantId = GetCurrentTenantId();
            if (!currentTenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context is required" });
            }

            var result = await _evaluatorService.BulkCreateEvaluatorsAsync(request, currentTenantId.Value);

            var totalProcessed = result.SuccessfullyCreated + result.UpdatedCount;
            _logger.LogInformation("Bulk processed {TotalProcessed}/{TotalRequested} evaluators for tenant {TenantId}: {Created} created, {Updated} updated, {Failed} failed",
                totalProcessed, result.TotalRequested, currentTenantId, result.SuccessfullyCreated, result.UpdatedCount, result.Failed);

            if (totalProcessed == 0)
            {
                return BadRequest(result);
            }

            if (result.Failed > 0)
            {
                return StatusCode(207, result); // Multi-Status for partial success
            }

            return CreatedAtAction(nameof(GetEvaluators), new { tenantId = currentTenantId }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for bulk creating evaluators");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for bulk creating evaluators");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating evaluators. Request: {@Request}", request);
            return StatusCode(500, new { message = "An error occurred while creating evaluators", details = ex.Message });
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

    /// <summary>
    /// Validate subject EmployeeIds for relationship creation
    /// </summary>
    /// <param name="employeeIds">List of subject EmployeeIds to validate</param>
    /// <returns>Detailed validation response with subject information</returns>
    [HttpPost("validate-subject-ids")]
    public async Task<ActionResult> ValidateSubjectIds([FromBody] List<string> employeeIds)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant not found" });
            }

            var validationResponse = await _relationshipService.ValidateEmployeeIdsDetailedAsync(employeeIds, tenantId.Value, isEvaluator: false);

            // Handle different response scenarios
            if (validationResponse.TotalRequested == 1)
            {
                var result = validationResponse.Results.First();
                if (result.IsValid)
                {
                    return Ok(result.Data);
                }
                else
                {
                    return NotFound(new { message = result.Message });
                }
            }
            else if (validationResponse.TotalRequested > 1)
            {
                // Multiple IDs - return 207 Multi-Status
                return StatusCode(207, validationResponse);
            }
            else
            {
                return BadRequest(new { message = "No employee IDs provided" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subject EmployeeIds");
            return StatusCode(500, new { message = "An error occurred while validating EmployeeIds" });
        }
    }
}
