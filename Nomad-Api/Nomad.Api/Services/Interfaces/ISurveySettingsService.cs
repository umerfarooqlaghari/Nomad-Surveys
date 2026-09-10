using Alpha.Api.DTOs.Request;
using Alpha.Api.DTOs.Response;

namespace Alpha.Api.Services.Interfaces;

public interface ITenantSettingsService
{
    Task<TenantSettingsResponse?> GetSettingsByTenantIdAsync(Guid tenantId);
    Task<TenantSettingsResponse> CreateSettingsAsync(CreateTenantSettingsRequest request, Guid tenantId);
    Task<TenantSettingsResponse> UpdateSettingsAsync(UpdateTenantSettingsRequest request, Guid tenantId);
}

