using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.Services.Interfaces;
using Nomad.Api.DTOs.Request;
using Nomad.Api.Services;

namespace Nomad.Api.Controllers;

/// <summary>
/// Controller for generating reports from HTML templates
/// </summary>
[ApiController]
[Route("{tenantSlug}/api/reports")]
[AuthorizeTenant]
public class ReportController : ControllerBase
{
    private readonly IReportTemplateService _reportTemplateService;
    private readonly IExcelReportService _excelReportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportTemplateService reportTemplateService,
        IExcelReportService excelReportService,
        ILogger<ReportController> logger)
    {
        _reportTemplateService = reportTemplateService;
        _excelReportService = excelReportService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Preview report HTML (optionally with Subject ID to show real subject name)
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult> PreviewReport(
        [FromBody] PreviewReportRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var html = await _reportTemplateService.GeneratePreviewHtmlAsync(
                request.CompanyName ?? "Company Name",
                request.CompanyLogoUrl,
                request.CoverImageUrl,
                request.PrimaryColor,
                request.SecondaryColor,
                request.TertiaryColor,
                request.SubjectId,
                request.SurveyId,
                tenantId);

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview: {Message}", ex.Message);
            return StatusCode(500, new { message = $"An error occurred while generating the preview: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generate report HTML
    /// </summary>
    [HttpPost("generate/html")]
    public async Task<ActionResult> GenerateReportHtml(
        [FromBody] ReportGenerationRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var html = await _reportTemplateService.GenerateReportHtmlAsync(
                request.SubjectId,
                request.SurveyId,
                tenantId.Value,
                request.CompanyName ?? "Company Name",
                request.CompanyLogoUrl,
                request.CoverImageUrl,
                request.PrimaryColor,
                request.SecondaryColor,
                request.TertiaryColor);

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report HTML for subject {SubjectId}", request.SubjectId);
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }

    /// <summary>
    /// Generate report PDF
    /// </summary>
    [HttpPost("generate/pdf")]
    public async Task<ActionResult> GenerateReportPdf(
        [FromBody] ReportGenerationRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var result = await _reportTemplateService.GenerateReportPdfAsync(
                request.SubjectId,
                request.SurveyId,
                tenantId.Value,
                request.CompanyName ?? "Company Name",
                request.CompanyLogoUrl,
                request.CoverImageUrl,
                request.PrimaryColor,
                request.SecondaryColor,
                request.TertiaryColor);

            return File(result.PdfBytes, "application/pdf", result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report PDF for subject {SubjectId}", request.SubjectId);
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }
    /// <summary>
    /// Generate Ratee Average Excel report
    /// </summary>
    [HttpPost("generate/excel/ratee-average")]
    public async Task<ActionResult> GenerateRateeAverageReportExcel(
        [FromBody] RateeAverageReportRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var (fileContent, fileName) = await _excelReportService.GenerateRateeAverageExcelAsync(
                request.SurveyId,
                tenantId.Value);

            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Ratee Average Excel report for Survey {SurveyId}", request.SurveyId);
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }

    /// <summary>
    /// Generate Subject Wise Heat Map Excel report
    /// </summary>
    [HttpPost("generate/excel/subject-heatmap")]
    public async Task<ActionResult> GenerateSubjectWiseHeatMapReportExcel(
        [FromBody] RateeAverageReportRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var (fileContent, fileName) = await _excelReportService.GenerateSubjectWiseHeatMapExcelAsync(
                request.SurveyId,
                tenantId.Value);

            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Subject Wise Heat Map Excel report for Survey {SurveyId}", request.SurveyId);
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }

    /// <summary>
    /// Generate Subject Wise Consolidated Excel report
    /// </summary>
    [HttpPost("generate/excel/subject-consolidated")]
    public async Task<ActionResult> GenerateSubjectWiseConsolidatedReportExcel(
        [FromBody] RateeAverageReportRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var (fileContent, fileName) = await _excelReportService.GenerateSubjectWiseConsolidatedExcelAsync(
                request.SurveyId,
                tenantId.Value);

            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Subject Wise Consolidated Excel report for Survey {SurveyId}", request.SurveyId);
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }
}

public class PreviewReportRequest
{
    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? TertiaryColor { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? SurveyId { get; set; }
}

public class ReportGenerationRequest
{
    public required Guid SubjectId { get; set; }
    public Guid? SurveyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? TertiaryColor { get; set; }
}

public class RateeAverageReportRequest
{
    public Guid SurveyId { get; set; }
}

