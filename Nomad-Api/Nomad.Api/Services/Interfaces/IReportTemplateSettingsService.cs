using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface IReportTemplateSettingsService
{
    Task<List<ReportTemplateSettingsResponse>> GetTemplateSettingsAsync(Guid tenantId, bool? isActive = null);
    Task<ReportTemplateSettingsResponse?> GetTemplateSettingsByIdAsync(Guid id, Guid tenantId);
    Task<ReportTemplateSettingsResponse?> GetDefaultTemplateSettingsAsync(Guid tenantId);
    Task<ReportTemplateSettingsResponse> CreateTemplateSettingsAsync(CreateReportTemplateSettingsRequest request, Guid tenantId);
    Task<ReportTemplateSettingsResponse?> UpdateTemplateSettingsAsync(Guid id, UpdateReportTemplateSettingsRequest request, Guid tenantId);
    Task<bool> DeleteTemplateSettingsAsync(Guid id, Guid tenantId);
    Task<bool> SetAsDefaultAsync(Guid id, Guid tenantId);
}


