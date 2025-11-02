using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;
using System.Security.Claims;

namespace Nomad.Api.Controllers;

/// <summary>
/// Controller for participant portal operations
/// </summary>
[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class ParticipantController : ControllerBase
{
    private readonly IParticipantService _participantService;
    private readonly ILogger<ParticipantController> _logger;

    public ParticipantController(
        IParticipantService participantService,
        ILogger<ParticipantController> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user ID from JWT claims
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    /// <summary>
    /// Get dashboard data for the logged-in participant
    /// GET: api/Participant/dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ParticipantDashboardResponse>> GetDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dashboard = await _participantService.GetDashboardAsync(userId);
            return Ok(dashboard);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access to dashboard: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard");
            return StatusCode(500, new { message = "An error occurred while retrieving dashboard data" });
        }
    }

    /// <summary>
    /// Get all assigned evaluations for the logged-in participant
    /// GET: api/Participant/evaluations?status=Pending&search=John
    /// </summary>
    [HttpGet("evaluations")]
    public async Task<ActionResult<List<AssignedEvaluationResponse>>> GetAssignedEvaluations(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var evaluations = await _participantService.GetAssignedEvaluationsAsync(userId, status, search);
            return Ok(evaluations);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access to evaluations: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned evaluations");
            return StatusCode(500, new { message = "An error occurred while retrieving evaluations" });
        }
    }

    /// <summary>
    /// Get evaluation form details for filling out
    /// GET: api/Participant/evaluations/{assignmentId}
    /// </summary>
    [HttpGet("evaluations/{assignmentId:guid}")]
    public async Task<ActionResult<EvaluationFormResponse>> GetEvaluationForm(Guid assignmentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var form = await _participantService.GetEvaluationFormAsync(userId, assignmentId);

            if (form == null)
            {
                return NotFound(new { message = "Evaluation assignment not found" });
            }

            return Ok(form);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access to evaluation form: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluation form for assignment {AssignmentId}", assignmentId);
            return StatusCode(500, new { message = "An error occurred while retrieving the evaluation form" });
        }
    }

    /// <summary>
    /// Save draft response (auto-save)
    /// POST: api/Participant/evaluations/{assignmentId}/save-draft
    /// </summary>
    [HttpPost("evaluations/{assignmentId:guid}/save-draft")]
    public async Task<ActionResult> SaveDraft(Guid assignmentId, [FromBody] SaveDraftRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _participantService.SaveDraftAsync(userId, assignmentId, request);

            if (!success)
            {
                return NotFound(new { message = "Evaluation assignment not found" });
            }

            return Ok(new { message = "Draft saved successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access to save draft: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving draft for assignment {AssignmentId}", assignmentId);
            return StatusCode(500, new { message = "An error occurred while saving the draft" });
        }
    }

    /// <summary>
    /// Submit completed evaluation
    /// POST: api/Participant/evaluations/{assignmentId}/submit
    /// </summary>
    [HttpPost("evaluations/{assignmentId:guid}/submit")]
    public async Task<ActionResult> SubmitEvaluation(Guid assignmentId, [FromBody] SubmitEvaluationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _participantService.SubmitEvaluationAsync(userId, assignmentId, request);

            if (!success)
            {
                return NotFound(new { message = "Evaluation assignment not found" });
            }

            return Ok(new { message = "Evaluation submitted successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access to submit evaluation: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting evaluation for assignment {AssignmentId}", assignmentId);
            return StatusCode(500, new { message = "An error occurred while submitting the evaluation" });
        }
    }

    /// <summary>
    /// Get submission history for the logged-in participant
    /// GET: api/Participant/submissions?search=John
    /// </summary>
    [HttpGet("submissions")]
    public async Task<ActionResult<List<SubmissionHistoryResponse>>> GetSubmissionHistory(
        [FromQuery] string? search = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var submissions = await _participantService.GetSubmissionHistoryAsync(userId, search);
            return Ok(submissions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access to submissions: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission history");
            return StatusCode(500, new { message = "An error occurred while retrieving submission history" });
        }
    }

    /// <summary>
    /// Get submission details (read-only view)
    /// GET: api/Participant/submissions/{submissionId}
    /// </summary>
    [HttpGet("submissions/{submissionId:guid}")]
    public async Task<ActionResult<SubmissionDetailResponse>> GetSubmissionDetail(Guid submissionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var submission = await _participantService.GetSubmissionDetailAsync(userId, submissionId);

            if (submission == null)
            {
                return NotFound(new { message = "Submission not found" });
            }

            return Ok(submission);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access to submission detail: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission detail for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while retrieving the submission" });
        }
    }

    /// <summary>
    /// Get all forms assigned to a specific evaluator
    /// GET: {tenantSlug}/api/Participant/evaluator/{evaluatorId}/forms
    /// </summary>
    [HttpGet("evaluator/{evaluatorId:guid}/forms")]
    public async Task<ActionResult<List<AssignedEvaluationResponse>>> GetEvaluatorForms(Guid evaluatorId)
    {
        try
        {
            var evaluations = await _participantService.GetEvaluatorFormsAsync(evaluatorId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forms for evaluator {EvaluatorId}", evaluatorId);
            return StatusCode(500, new { message = "An error occurred while retrieving evaluator forms" });
        }
    }
}

