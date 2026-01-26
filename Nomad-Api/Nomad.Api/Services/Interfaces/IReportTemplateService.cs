using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

/// <summary>
/// Result object containing PDF bytes and suggested filename
/// </summary>
public class PdfGenerationResult
{
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "report.pdf";
    public string SubjectName { get; set; } = string.Empty;
}

public interface IReportTemplateService
{
    Task<string> GeneratePreviewHtmlAsync(
        string companyName,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null,
        Guid? subjectId = null,
        Guid? surveyId = null,
        Guid? tenantId = null);

    Task<string> GenerateReportHtmlAsync(
        Guid subjectId,
        Guid? surveyId,
        Guid tenantId,
        string companyName,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null);

    Task<PdfGenerationResult> GenerateReportPdfAsync(
        Guid subjectId,
        Guid? surveyId,
        Guid tenantId,
        string companyName,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null);

    Task<byte[]> GenerateChartImageAsync(
        string chartType,
        Dictionary<string, object> chartData,
        int width = 800,
        int height = 600);
}

