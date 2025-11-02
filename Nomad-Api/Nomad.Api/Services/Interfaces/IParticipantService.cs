using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

/// <summary>
/// Service interface for participant portal operations
/// </summary>
public interface IParticipantService
{
    /// <summary>
    /// Get dashboard data for the logged-in participant
    /// </summary>
    Task<ParticipantDashboardResponse> GetDashboardAsync(Guid userId);

    /// <summary>
    /// Get all assigned evaluations for the logged-in participant
    /// </summary>
    Task<List<AssignedEvaluationResponse>> GetAssignedEvaluationsAsync(Guid userId, string? status = null, string? search = null);

    /// <summary>
    /// Get evaluation form details for filling out
    /// </summary>
    Task<EvaluationFormResponse?> GetEvaluationFormAsync(Guid userId, Guid assignmentId);

    /// <summary>
    /// Save draft response (auto-save)
    /// </summary>
    Task<bool> SaveDraftAsync(Guid userId, Guid assignmentId, SaveDraftRequest request);

    /// <summary>
    /// Submit completed evaluation
    /// </summary>
    Task<bool> SubmitEvaluationAsync(Guid userId, Guid assignmentId, SubmitEvaluationRequest request);

    /// <summary>
    /// Get submission history for the logged-in participant
    /// </summary>
    Task<List<SubmissionHistoryResponse>> GetSubmissionHistoryAsync(Guid userId, string? search = null);

    /// <summary>
    /// Get submission details (read-only view)
    /// </summary>
    Task<SubmissionDetailResponse?> GetSubmissionDetailAsync(Guid userId, Guid submissionId);

    /// <summary>
    /// Get all forms assigned to a specific evaluator
    /// </summary>
    Task<List<AssignedEvaluationResponse>> GetEvaluatorFormsAsync(Guid evaluatorId);
}

