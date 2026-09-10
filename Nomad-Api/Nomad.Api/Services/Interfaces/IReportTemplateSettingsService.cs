using Alpha.Api.DTOs.Request;
using Alpha.Api.DTOs.Response;

namespace Alpha.Api.Services.Interfaces;

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


