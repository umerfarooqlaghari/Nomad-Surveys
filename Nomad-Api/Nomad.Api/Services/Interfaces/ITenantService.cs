using Alpha.Api.DTOs.Request;
using Alpha.Api.DTOs.Response;

namespace Alpha.Api.Services.Interfaces;

public interface ITenantService
{
    Task<TenantResponse> CreateTenantAsync(CreateTenantRequest request);
    Task<TenantResponse?> GetTenantByIdAsync(Guid tenantId);
    Task<TenantResponse?> GetTenantBySlugAsync(string slug);
    Task<List<TenantListResponse>> GetTenantsAsync();
    Task<bool> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request);
    Task<bool> DeactivateTenantAsync(Guid tenantId);
    Task<bool> ActivateTenantAsync(Guid tenantId);
    Task<CompanyResponse?> GetCompanyByTenantIdAsync(Guid tenantId);
    Task<bool> UpdateCompanyAsync(Guid tenantId, CreateCompanyRequest request);
}
