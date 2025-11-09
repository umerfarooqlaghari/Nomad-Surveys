using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface ICompetencyService
{
    Task<List<CompetencyListResponse>> GetCompetenciesAsync(Guid? tenantId = null, Guid? clusterId = null);
    Task<CompetencyResponse?> GetCompetencyByIdAsync(Guid competencyId);
    Task<CompetencyResponse> CreateCompetencyAsync(CreateCompetencyRequest request, Guid tenantId);
    Task<CompetencyResponse?> UpdateCompetencyAsync(Guid competencyId, UpdateCompetencyRequest request);
    Task<bool> DeleteCompetencyAsync(Guid competencyId);
    Task<bool> CompetencyExistsAsync(Guid competencyId, Guid? tenantId = null);
}

