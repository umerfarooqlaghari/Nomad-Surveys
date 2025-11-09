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
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly IRelationshipService _relationshipService;
    private readonly ILogger<SubjectsController> _logger;

    public SubjectsController(ISubjectService subjectService, IRelationshipService relationshipService, ILogger<SubjectsController> logger)
    {
        _subjectService = subjectService;
        _relationshipService = relationshipService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all subjects with optional tenant filtering
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter. If null, shows all subjects (SuperAdmin only)</param>
    /// <returns>List of subjects</returns>
    [HttpGet]
    public async Task<ActionResult<List<SubjectListResponse>>> GetSubjects([FromQuery] Guid? tenantId = null)
    {
        try
        {
            var currentTenantId = GetCurrentTenantId();
            
            // If user is not SuperAdmin, they can only see their own tenant's subjects
            if (currentTenantId.HasValue && tenantId.HasValue && tenantId != currentTenantId)
            {
                return Forbid("You can only access subjects from your own tenant");
            }

            // Use current tenant if no specific tenant requested and user is not SuperAdmin
            var filterTenantId = tenantId ?? currentTenantId;

            var subjects = await _subjectService.GetSubjectsAsync(filterTenantId);
            
            _logger.LogInformation("Retrieved {Count} subjects for tenant {TenantId}", subjects.Count, filterTenantId);
            
            return Ok(subjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subjects");
            return StatusCode(500, new { message = "An error occurred while retrieving subjects" });
        }
    }

    /// <summary>
    /// Get a specific subject by ID
    /// </summary>
    /// <param name="id">Subject ID</param>
    /// <returns>Subject details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectResponse>> GetSubject(Guid id)
    {
        try
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            
            if (subject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            
            // Ensure user can only access subjects from their tenant (unless SuperAdmin)
            if (currentTenantId.HasValue && subject.TenantId != currentTenantId)
            {
                return Forbid("You can only access subjects from your own tenant");
            }

            _logger.LogInformation("Retrieved subject {SubjectId}", id);
            
            return Ok(subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subject {SubjectId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the subject" });
        }
    }

    /// <summary>
    /// Bulk create subjects (works for single subject as well)
    /// </summary>
    /// <param name="request">Bulk create request with list of subjects</param>
    /// <returns>Bulk creation result</returns>
    [HttpPost("bulk")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<BulkCreateResponse>> BulkCreateSubjects([FromBody] BulkCreateSubjectsRequest request)
    {
        try
        {
            _logger.LogInformation("Received bulk create subjects request with {Count} subjects", request?.Subjects?.Count ?? 0);

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

            var result = await _subjectService.BulkCreateSubjectsAsync(request, currentTenantId.Value);

            var totalProcessed = result.SuccessfullyCreated + result.UpdatedCount;
            _logger.LogInformation("Bulk processed {TotalProcessed}/{TotalRequested} subjects for tenant {TenantId}: {Created} created, {Updated} updated, {Failed} failed",
                totalProcessed, result.TotalRequested, currentTenantId, result.SuccessfullyCreated, result.UpdatedCount, result.Failed);

            if (totalProcessed == 0)
            {
                return BadRequest(result);
            }

            if (result.Failed > 0)
            {
                return StatusCode(207, result); // Multi-Status for partial success
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for bulk creating subjects");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for bulk creating subjects");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating subjects. Request: {@Request}", request);
            return StatusCode(500, new { message = "An error occurred while creating subjects", details = ex.Message });
        }
    }

    /// <summary>
    /// Update a subject
    /// </summary>
    /// <param name="id">Subject ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated subject</returns>
    [HttpPut("{id}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<SubjectResponse>> UpdateSubject(Guid id, [FromBody] UpdateSubjectRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if subject exists and user has access
            var existingSubject = await _subjectService.GetSubjectByIdAsync(id);
            if (existingSubject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && existingSubject.TenantId != currentTenantId)
            {
                return Forbid("You can only update subjects from your own tenant");
            }

            var updatedSubject = await _subjectService.UpdateSubjectAsync(id, request);
            
            if (updatedSubject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            _logger.LogInformation("Updated subject {SubjectId}", id);
            
            return Ok(updatedSubject);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating subject {SubjectId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject {SubjectId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the subject" });
        }
    }

    /// <summary>
    /// Delete a subject (soft delete)
    /// </summary>
    /// <param name="id">Subject ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult> DeleteSubject(Guid id)
    {
        try
        {
            // Check if subject exists and user has access
            var existingSubject = await _subjectService.GetSubjectByIdAsync(id);
            if (existingSubject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && existingSubject.TenantId != currentTenantId)
            {
                return Forbid("You can only delete subjects from your own tenant");
            }

            var deleted = await _subjectService.DeleteSubjectAsync(id);
            
            if (!deleted)
            {
                return NotFound(new { message = "Subject not found" });
            }

            _logger.LogInformation("Deleted subject {SubjectId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {SubjectId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the subject" });
        }
    }

    /// <summary>
    /// Validate evaluator EmployeeIds for relationship creation
    /// </summary>
    /// <param name="employeeIds">List of evaluator EmployeeIds to validate</param>
    /// <returns>Detailed validation response with evaluator information</returns>
    [HttpPost("validate-evaluator-ids")]
    public async Task<ActionResult> ValidateEvaluatorIds([FromBody] List<string> employeeIds)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant not found" });
            }

            var validationResponse = await _relationshipService.ValidateEmployeeIdsDetailedAsync(employeeIds, tenantId.Value, isEvaluator: true);

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
            _logger.LogError(ex, "Error validating evaluator EmployeeIds");
            return StatusCode(500, new { message = "An error occurred while validating EmployeeIds" });
        }
    }
}
