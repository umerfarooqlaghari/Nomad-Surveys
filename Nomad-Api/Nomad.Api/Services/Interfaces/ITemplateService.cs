using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

/// <summary>
/// Service for managing report templates
/// </summary>
public interface ITemplateService
{
    Task<TemplateResponse?> GetTemplateByIdAsync(Guid templateId, Guid tenantId);
    Task<List<TemplateListResponse>> GetTemplatesAsync(Guid tenantId, bool? isActive = null);
    Task<TemplateResponse> CreateTemplateAsync(CreateTemplateRequest request, Guid tenantId);
    Task<TemplateResponse?> UpdateTemplateAsync(Guid templateId, UpdateTemplateRequest request, Guid tenantId);
    Task<bool> DeleteTemplateAsync(Guid templateId, Guid tenantId);
    Task<GeneratedReportResponse> GenerateReportAsync(GenerateReportRequest request, Guid tenantId);
}


