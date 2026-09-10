using Alpha.Api.DTOs.Request;
using Alpha.Api.DTOs.Response;

namespace Alpha.Api.Services.Interfaces;

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
