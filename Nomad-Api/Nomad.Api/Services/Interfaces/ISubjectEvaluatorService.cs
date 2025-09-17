using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface ISubjectEvaluatorService
{
    Task<AssignmentResponse> AssignEvaluatorsToSubjectAsync(Guid subjectId, AssignEvaluatorsToSubjectRequest request);
    Task<AssignmentResponse> AssignSubjectsToEvaluatorAsync(Guid evaluatorId, AssignSubjectsToEvaluatorRequest request);
    Task<bool> RemoveAssignmentAsync(Guid subjectId, Guid evaluatorId);
    Task<List<SubjectEvaluatorResponse>> GetSubjectEvaluatorsAsync(Guid subjectId);
    Task<List<SubjectEvaluatorResponse>> GetEvaluatorSubjectsAsync(Guid evaluatorId);
    Task<bool> AssignmentExistsAsync(Guid subjectId, Guid evaluatorId);
}
