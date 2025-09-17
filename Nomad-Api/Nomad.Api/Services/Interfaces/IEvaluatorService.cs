using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface IEvaluatorService
{
    Task<List<EvaluatorListResponse>> GetEvaluatorsAsync(Guid? tenantId = null);
    Task<EvaluatorResponse?> GetEvaluatorByIdAsync(Guid evaluatorId);
    Task<BulkCreateResponse> BulkCreateEvaluatorsAsync(BulkCreateEvaluatorsRequest request, Guid tenantId);
    Task<EvaluatorResponse?> UpdateEvaluatorAsync(Guid evaluatorId, UpdateEvaluatorRequest request);
    Task<bool> DeleteEvaluatorAsync(Guid evaluatorId);
    Task<bool> EvaluatorExistsAsync(Guid evaluatorId, Guid? tenantId = null);

    Task<bool> EvaluatorExistsByEmailAsync(string email, Guid tenantId, Guid? excludeId = null);
}
