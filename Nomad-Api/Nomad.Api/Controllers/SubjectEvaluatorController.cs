using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api")]
[AuthorizeTenant]
public class SubjectEvaluatorController : ControllerBase
{
    private readonly ISubjectEvaluatorService _subjectEvaluatorService;
    private readonly ISubjectService _subjectService;
    private readonly IEvaluatorService _evaluatorService;
    private readonly ILogger<SubjectEvaluatorController> _logger;

    public SubjectEvaluatorController(
        ISubjectEvaluatorService subjectEvaluatorService,
        ISubjectService subjectService,
        IEvaluatorService evaluatorService,
        ILogger<SubjectEvaluatorController> logger)
    {
        _subjectEvaluatorService = subjectEvaluatorService;
        _subjectService = subjectService;
        _evaluatorService = evaluatorService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Assign evaluator(s) to a subject
    /// </summary>
    /// <param name="subjectId">Subject ID</param>
    /// <param name="request">Assignment request with evaluator IDs and relationships</param>
    /// <returns>Assignment result</returns>
    [HttpPost("subjects/{subjectId}/evaluators")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<AssignmentResponse>> AssignEvaluatorsToSubject(
        Guid subjectId, 
        [FromBody] AssignEvaluatorsToSubjectRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify subject exists and user has access
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && subject.TenantId != currentTenantId)
            {
                return Forbid("You can only assign evaluators to subjects from your own tenant");
            }

            var result = await _subjectEvaluatorService.AssignEvaluatorsToSubjectAsync(subjectId, request);
            
            _logger.LogInformation("Assignment result for subject {SubjectId}: {Success}, {Message}", 
                subjectId, result.Success, result.Message);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning evaluators to subject {SubjectId}", subjectId);
            return StatusCode(500, new { message = "An error occurred while assigning evaluators" });
        }
    }

    /// <summary>
    /// Assign subject(s) to an evaluator
    /// </summary>
    /// <param name="evaluatorId">Evaluator ID</param>
    /// <param name="request">Assignment request with subject IDs and relationships</param>
    /// <returns>Assignment result</returns>
    [HttpPost("evaluators/{evaluatorId}/subjects")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<AssignmentResponse>> AssignSubjectsToEvaluator(
        Guid evaluatorId, 
        [FromBody] AssignSubjectsToEvaluatorRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify evaluator exists and user has access
            var evaluator = await _evaluatorService.GetEvaluatorByIdAsync(evaluatorId);
            if (evaluator == null)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && evaluator.TenantId != currentTenantId)
            {
                return Forbid("You can only assign subjects to evaluators from your own tenant");
            }

            var result = await _subjectEvaluatorService.AssignSubjectsToEvaluatorAsync(evaluatorId, request);
            
            _logger.LogInformation("Assignment result for evaluator {EvaluatorId}: {Success}, {Message}", 
                evaluatorId, result.Success, result.Message);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning subjects to evaluator {EvaluatorId}", evaluatorId);
            return StatusCode(500, new { message = "An error occurred while assigning subjects" });
        }
    }

    /// <summary>
    /// Update relationship type between a subject and evaluator
    /// </summary>
    /// <param name="subjectId">Subject ID</param>
    /// <param name="evaluatorId">Evaluator ID</param>
    /// <param name="request">Update request with new relationship type</param>
    /// <returns>Updated assignment</returns>
    [HttpPut("subjects/{subjectId}/evaluators/{evaluatorId}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<SubjectEvaluatorResponse>> UpdateAssignment(
        Guid subjectId,
        Guid evaluatorId,
        [FromBody] UpdateRelationshipRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify both subject and evaluator exist and user has access
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            var evaluator = await _evaluatorService.GetEvaluatorByIdAsync(evaluatorId);
            if (evaluator == null)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue &&
                (subject.TenantId != currentTenantId || evaluator.TenantId != currentTenantId))
            {
                return Forbid("You can only update assignments within your own tenant");
            }

            var updated = await _subjectEvaluatorService.UpdateAssignmentAsync(subjectId, evaluatorId, request.Relationship);

            if (updated == null)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            _logger.LogInformation("Updated assignment between subject {SubjectId} and evaluator {EvaluatorId} to relationship {Relationship}",
                subjectId, evaluatorId, request.Relationship);

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment between subject {SubjectId} and evaluator {EvaluatorId}",
                subjectId, evaluatorId);
            return StatusCode(500, new { message = "An error occurred while updating the assignment" });
        }
    }

    /// <summary>
    /// Remove assignment between a subject and evaluator
    /// </summary>
    /// <param name="subjectId">Subject ID</param>
    /// <param name="evaluatorId">Evaluator ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("subjects/{subjectId}/evaluators/{evaluatorId}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult> RemoveAssignment(Guid subjectId, Guid evaluatorId)
    {
        try
        {
            // Verify both subject and evaluator exist and user has access
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            var evaluator = await _evaluatorService.GetEvaluatorByIdAsync(evaluatorId);
            if (evaluator == null)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && 
                (subject.TenantId != currentTenantId || evaluator.TenantId != currentTenantId))
            {
                return Forbid("You can only remove assignments within your own tenant");
            }

            var removed = await _subjectEvaluatorService.RemoveAssignmentAsync(subjectId, evaluatorId);
            
            if (!removed)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            _logger.LogInformation("Removed assignment between subject {SubjectId} and evaluator {EvaluatorId}", 
                subjectId, evaluatorId);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing assignment between subject {SubjectId} and evaluator {EvaluatorId}", 
                subjectId, evaluatorId);
            return StatusCode(500, new { message = "An error occurred while removing the assignment" });
        }
    }

    /// <summary>
    /// Get all evaluators assigned to a subject
    /// </summary>
    /// <param name="subjectId">Subject ID</param>
    /// <returns>List of subject-evaluator assignments</returns>
    [HttpGet("subjects/{subjectId}/evaluators")]
    public async Task<ActionResult<List<SubjectEvaluatorResponse>>> GetSubjectEvaluators(Guid subjectId)
    {
        try
        {
            // Verify subject exists and user has access
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && subject.TenantId != currentTenantId)
            {
                return Forbid("You can only access subjects from your own tenant");
            }

            var assignments = await _subjectEvaluatorService.GetSubjectEvaluatorsAsync(subjectId);
            
            _logger.LogInformation("Retrieved {Count} evaluator assignments for subject {SubjectId}", 
                assignments.Count, subjectId);
            
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evaluators for subject {SubjectId}", subjectId);
            return StatusCode(500, new { message = "An error occurred while retrieving evaluator assignments" });
        }
    }

    /// <summary>
    /// Get all subjects assigned to an evaluator
    /// </summary>
    /// <param name="evaluatorId">Evaluator ID</param>
    /// <returns>List of subject-evaluator assignments</returns>
    [HttpGet("evaluators/{evaluatorId}/subjects")]
    public async Task<ActionResult<List<SubjectEvaluatorResponse>>> GetEvaluatorSubjects(Guid evaluatorId)
    {
        try
        {
            // Verify evaluator exists and user has access
            var evaluator = await _evaluatorService.GetEvaluatorByIdAsync(evaluatorId);
            if (evaluator == null)
            {
                return NotFound(new { message = "Evaluator not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && evaluator.TenantId != currentTenantId)
            {
                return Forbid("You can only access evaluators from your own tenant");
            }

            var assignments = await _subjectEvaluatorService.GetEvaluatorSubjectsAsync(evaluatorId);
            
            _logger.LogInformation("Retrieved {Count} subject assignments for evaluator {EvaluatorId}", 
                assignments.Count, evaluatorId);
            
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subjects for evaluator {EvaluatorId}", evaluatorId);
            return StatusCode(500, new { message = "An error occurred while retrieving subject assignments" });
        }
    }
}
