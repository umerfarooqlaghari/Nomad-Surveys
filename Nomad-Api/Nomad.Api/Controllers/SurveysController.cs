using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveyService;
    private readonly ISurveyAssignmentService _surveyAssignmentService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(
        ISurveyService surveyService,
        ISurveyAssignmentService surveyAssignmentService,
        IMemoryCache cache,
        ILogger<SurveysController> logger)
    {
        _surveyService = surveyService;
        _surveyAssignmentService = surveyAssignmentService;
        _cache = cache;
        _logger = logger;
    }

    private void ClearEmailingListCache(Guid tenantId)
    {
        var cacheKey = $"EmailingList_{tenantId}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("Cleared emailing list cache for tenant {TenantId} from SurveysController", tenantId);
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all surveys for the current tenant
    /// </summary>
    /// <returns>List of surveys</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<SurveyListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SurveyListResponse>>> GetSurveys()
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var surveys = await _surveyService.GetSurveysAsync(tenantId);
            return Ok(surveys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving surveys");
            return StatusCode(500, new { error = "An error occurred while retrieving surveys" });
        }
    }

    /// <summary>
    /// Get a specific survey by ID
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <returns>Survey details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SurveyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SurveyResponse>> GetSurveyById(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var survey = await _surveyService.GetSurveyByIdAsync(id);
            
            if (survey == null)
            {
                return NotFound(new { error = $"Survey with ID {id} not found" });
            }

            // Verify the survey belongs to the current tenant
            if (survey.TenantId != tenantId)
            {
                return NotFound(new { error = $"Survey with ID {id} not found" });
            }

            return Ok(survey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving survey {SurveyId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the survey" });
        }
    }

    /// <summary>
    /// Create a new survey
    /// </summary>
    /// <param name="request">Survey creation request</param>
    /// <returns>Created survey</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SurveyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SurveyResponse>> CreateSurvey([FromBody] CreateSurveyRequest request)
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

            var survey = await _surveyService.CreateSurveyAsync(request, tenantId.Value);

            return StatusCode(201, survey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating survey");
            return StatusCode(500, new { error = "An error occurred while creating the survey" });
        }
    }

    /// <summary>
    /// Update an existing survey
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <param name="request">Survey update request</param>
    /// <returns>Updated survey</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SurveyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SurveyResponse>> UpdateSurvey(Guid id, [FromBody] UpdateSurveyRequest request)
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

            // Verify the survey exists and belongs to the current tenant
            var existingSurvey = await _surveyService.GetSurveyByIdAsync(id);
            if (existingSurvey == null || existingSurvey.TenantId != tenantId)
            {
                return NotFound(new { error = $"Survey with ID {id} not found" });
            }

            var survey = await _surveyService.UpdateSurveyAsync(id, request);
            
            if (survey == null)
            {
                return NotFound(new { error = $"Survey with ID {id} not found" });
            }

            return Ok(survey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating survey {SurveyId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the survey" });
        }
    }

    /// <summary>
    /// Delete a survey
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteSurvey(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify the survey exists and belongs to the current tenant
            var existingSurvey = await _surveyService.GetSurveyByIdAsync(id);
            if (existingSurvey == null || existingSurvey.TenantId != tenantId)
            {
                return NotFound(new { error = $"Survey with ID {id} not found" });
            }

            var deleted = await _surveyService.DeleteSurveyAsync(id);
            
            if (!deleted)
            {
                return NotFound(new { error = $"Survey with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting survey {SurveyId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the survey" });
        }
    }

    /// <summary>
    /// Assign survey to subject-evaluator relationships
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <param name="request">Assignment request with SubjectEvaluatorIds</param>
    /// <returns>Assignment result</returns>
    [HttpPost("{id}/assign-relationships")]
    [ProducesResponseType(typeof(SurveyAssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SurveyAssignmentResponse>> AssignSurveyToRelationships(Guid id, [FromBody] AssignSurveyRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            if (request.SubjectEvaluatorIds == null || request.SubjectEvaluatorIds.Count == 0)
            {
                return BadRequest(new { error = "SubjectEvaluatorIds cannot be empty" });
            }

            var result = await _surveyAssignmentService.AssignSurveyToRelationshipsAsync(id, request);

            if (!result.Success)
            {
                return NotFound(new { error = result.Message });
            }

            if (tenantId.HasValue)
            {
                ClearEmailingListCache(tenantId.Value);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning survey {SurveyId} to relationships", id);
            return StatusCode(500, new { error = "An error occurred while assigning survey" });
        }
    }

    /// <summary>
    /// Assign survey relationships based on a CSV payload
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <param name="request">CSV assignment payload</param>
    /// <returns>Assignment result</returns>
    [HttpPost("{id}/assign-csv")]
    [ProducesResponseType(typeof(SurveyAssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SurveyAssignmentResponse>> AssignSurveyFromCsv(Guid id, [FromBody] AssignSurveyCsvRequest request)
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

            if (request.Rows == null || request.Rows.Count == 0)
            {
                return BadRequest(new { error = "CSV payload must include at least one row" });
            }

            var result = await _surveyAssignmentService.AssignSurveyRelationshipsFromCsvAsync(id, request);

            if (!result.Success && result.AssignedCount == 0)
            {
                return BadRequest(new { error = result.Message, details = result.Errors });
            }

            if (tenantId.HasValue)
            {
                ClearEmailingListCache(tenantId.Value);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning survey {SurveyId} relationships from CSV", id);
            return StatusCode(500, new { error = "An error occurred while assigning survey relationships from CSV" });
        }
    }

    /// <summary>
    /// Unassign survey from subject-evaluator relationships
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <param name="request">Unassignment request with SubjectEvaluatorIds</param>
    /// <returns>Unassignment result</returns>
    [HttpPost("{id}/unassign-relationships")]
    [ProducesResponseType(typeof(SurveyAssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SurveyAssignmentResponse>> UnassignSurveyFromRelationships(Guid id, [FromBody] UnassignSurveyRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            if (request.SubjectEvaluatorIds == null || request.SubjectEvaluatorIds.Count == 0)
            {
                return BadRequest(new { error = "SubjectEvaluatorIds cannot be empty" });
            }

            var result = await _surveyAssignmentService.UnassignSurveyFromRelationshipsAsync(id, request);

            if (tenantId.HasValue)
            {
                ClearEmailingListCache(tenantId.Value);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning survey {SurveyId} from relationships", id);
            return StatusCode(500, new { error = "An error occurred while unassigning survey" });
        }
    }

    /// <summary>
    /// Get relationships already assigned to this survey
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <param name="search">Optional search term</param>
    /// <param name="relationshipType">Optional relationship type filter</param>
    /// <returns>List of assigned relationships</returns>
    [HttpGet("{id}/assigned-relationships")]
    [ProducesResponseType(typeof(List<AssignedRelationshipResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AssignedRelationshipResponse>>> GetAssignedRelationships(
        Guid id,
        [FromQuery] string? search = null,
        [FromQuery] string? relationshipType = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var relationships = await _surveyAssignmentService.GetAssignedRelationshipsAsync(id, search, relationshipType);
            return Ok(relationships);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned relationships for survey {SurveyId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving assigned relationships" });
        }
    }

    /// <summary>
    /// Get relationships available for assignment to this survey
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <param name="search">Optional search term</param>
    /// <param name="relationshipType">Optional relationship type filter</param>
    /// <returns>List of available relationships</returns>
    [HttpGet("{id}/available-relationships")]
    [ProducesResponseType(typeof(List<AvailableRelationshipResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AvailableRelationshipResponse>>> GetAvailableRelationships(
        Guid id,
        [FromQuery] string? search = null,
        [FromQuery] string? relationshipType = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var relationships = await _surveyAssignmentService.GetAvailableRelationshipsAsync(id, search, relationshipType);
            return Ok(relationships);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available relationships for survey {SurveyId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving available relationships" });
        }
    }

    /// <summary>
    /// Get count of relationships assigned to this survey
    /// </summary>
    /// <param name="id">Survey ID</param>
    /// <returns>Assignment count</returns>
    [HttpGet("{id}/assignment-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> GetAssignmentCount(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var count = await _surveyAssignmentService.GetAssignmentCountAsync(id);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignment count for survey {SurveyId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving assignment count" });
        }
    }
}

