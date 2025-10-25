using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveyService;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(ISurveyService surveyService, ILogger<SurveysController> logger)
    {
        _surveyService = surveyService;
        _logger = logger;
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
}

