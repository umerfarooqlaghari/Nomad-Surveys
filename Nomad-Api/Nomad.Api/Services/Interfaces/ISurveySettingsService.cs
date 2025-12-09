using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface ITenantSettingsService
{
    Task<TenantSettingsResponse?> GetSettingsByTenantIdAsync(Guid tenantId);
    Task<TenantSettingsResponse> CreateSettingsAsync(CreateTenantSettingsRequest request, Guid tenantId);
    Task<TenantSettingsResponse> UpdateSettingsAsync(UpdateTenantSettingsRequest request, Guid tenantId);
}

