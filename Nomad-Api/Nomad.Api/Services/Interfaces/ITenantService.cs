using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

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
