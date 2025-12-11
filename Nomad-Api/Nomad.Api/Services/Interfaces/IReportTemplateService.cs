using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

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
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null);

    Task<byte[]> GenerateReportPdfAsync(
        Guid subjectId,
        Guid? surveyId,
        Guid tenantId,
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

