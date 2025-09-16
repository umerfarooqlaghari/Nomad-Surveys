using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface ISubjectService
{
    Task<List<SubjectListResponse>> GetSubjectsAsync(Guid? tenantId = null);
    Task<SubjectResponse?> GetSubjectByIdAsync(Guid subjectId);
    Task<BulkCreateResponse> BulkCreateSubjectsAsync(BulkCreateSubjectsRequest request, Guid tenantId);
    Task<SubjectResponse?> UpdateSubjectAsync(Guid subjectId, UpdateSubjectRequest request);
    Task<bool> DeleteSubjectAsync(Guid subjectId);
    Task<bool> SubjectExistsAsync(Guid subjectId, Guid? tenantId = null);

    Task<bool> SubjectExistsByEmailAsync(string email, Guid tenantId, Guid? excludeId = null);
}
