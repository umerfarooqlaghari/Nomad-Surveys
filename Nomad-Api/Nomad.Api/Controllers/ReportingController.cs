using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

/// <summary>
/// Controller for reporting and template management
/// </summary>
[ApiController]
[Route("{tenantSlug}/api/reporting")]
[AuthorizeTenant]
public class ReportingController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly ITemplateService _templateService;
    private readonly ILogger<ReportingController> _logger;

    public ReportingController(
        IReportingService reportingService,
        ITemplateService templateService,
        ILogger<ReportingController> logger)
    {
        _reportingService = reportingService;
        _templateService = templateService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    #region Reporting Endpoints

    /// <summary>
    /// Get subject report
    /// </summary>
    [HttpGet("subjects/{subjectId}/report")]
    public async Task<ActionResult<SubjectReportResponse>> GetSubjectReport(
        Guid subjectId,
        [FromQuery] Guid? surveyId = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var report = await _reportingService.GetSubjectReportAsync(subjectId, surveyId, tenantId.Value);
            
            if (report == null)
            {
                return NotFound(new { message = "Report not found for the specified subject and survey" });
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subject report for {SubjectId}", subjectId);
            return StatusCode(500, new { message = "An error occurred while retrieving the report" });
        }
    }

    /// <summary>
    /// Get self vs evaluator comparison
    /// </summary>
    [HttpGet("subjects/{subjectId}/comparison/self-vs-evaluator")]
    public async Task<ActionResult<SelfVsEvaluatorComparisonResponse>> GetSelfVsEvaluatorComparison(
        Guid subjectId,
        [FromQuery] Guid? surveyId = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var comparison = await _reportingService.GetSelfVsEvaluatorComparisonAsync(subjectId, surveyId, tenantId.Value);
            
            if (comparison == null)
            {
                return NotFound(new { message = "Comparison data not found" });
            }

            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving self vs evaluator comparison for {SubjectId}", subjectId);
            return StatusCode(500, new { message = "An error occurred while retrieving the comparison" });
        }
    }

    /// <summary>
    /// Get organization comparison
    /// </summary>
    [HttpGet("subjects/{subjectId}/comparison/organization")]
    public async Task<ActionResult<OrganizationComparisonResponse>> GetOrganizationComparison(
        Guid subjectId,
        [FromQuery] Guid? surveyId = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var comparison = await _reportingService.GetOrganizationComparisonAsync(subjectId, surveyId, tenantId.Value);
            
            if (comparison == null)
            {
                return NotFound(new { message = "Organization comparison data not found" });
            }

            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization comparison for {SubjectId}", subjectId);
            return StatusCode(500, new { message = "An error occurred while retrieving the comparison" });
        }
    }

    /// <summary>
    /// Get comprehensive report
    /// </summary>
    [HttpGet("subjects/{subjectId}/comprehensive")]
    public async Task<ActionResult<ComprehensiveReportResponse>> GetComprehensiveReport(
        Guid subjectId,
        [FromQuery] Guid? surveyId = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var report = await _reportingService.GetComprehensiveReportAsync(subjectId, surveyId, tenantId.Value);
            
            if (report == null)
            {
                return NotFound(new { message = "Comprehensive report not found" });
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comprehensive report for {SubjectId}", subjectId);
            return StatusCode(500, new { message = "An error occurred while retrieving the comprehensive report" });
        }
    }

    #endregion

    #region Template Endpoints

    /// <summary>
    /// Get all templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<List<TemplateListResponse>>> GetTemplates([FromQuery] bool? isActive = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var templates = await _templateService.GetTemplatesAsync(tenantId.Value, isActive);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates");
            return StatusCode(500, new { message = "An error occurred while retrieving templates" });
        }
    }

    /// <summary>
    /// Get template by ID
    /// </summary>
    [HttpGet("templates/{templateId}")]
    public async Task<ActionResult<TemplateResponse>> GetTemplate(Guid templateId)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var template = await _templateService.GetTemplateByIdAsync(templateId, tenantId.Value);
            
            if (template == null)
            {
                return NotFound(new { message = "Template not found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred while retrieving the template" });
        }
    }

    /// <summary>
    /// Create a new template
    /// </summary>
    [HttpPost("templates")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<TemplateResponse>> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var template = await _templateService.CreateTemplateAsync(request, tenantId.Value);
            return CreatedAtAction(nameof(GetTemplate), new { templateId = template.Id }, template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, new { message = "An error occurred while creating the template" });
        }
    }

    /// <summary>
    /// Update a template
    /// </summary>
    [HttpPut("templates/{templateId}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<TemplateResponse>> UpdateTemplate(
        Guid templateId,
        [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var template = await _templateService.UpdateTemplateAsync(templateId, request, tenantId.Value);
            
            if (template == null)
            {
                return NotFound(new { message = "Template not found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred while updating the template" });
        }
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    [HttpDelete("templates/{templateId}")]
    [AuthorizeTenantAdmin]
    public async Task<IActionResult> DeleteTemplate(Guid templateId)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var deleted = await _templateService.DeleteTemplateAsync(templateId, tenantId.Value);
            
            if (!deleted)
            {
                return NotFound(new { message = "Template not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred while deleting the template" });
        }
    }

    /// <summary>
    /// Generate a PDF report from a template
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult> GenerateReport([FromBody] GenerateReportRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var report = await _templateService.GenerateReportAsync(request, tenantId.Value);
            
            return File(report.PdfContent, report.ContentType, report.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }

    #endregion
}


