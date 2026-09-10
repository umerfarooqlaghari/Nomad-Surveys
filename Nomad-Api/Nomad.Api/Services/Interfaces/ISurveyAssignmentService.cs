using Alpha.Api.DTOs.Request;
using Alpha.Api.DTOs.Response;

namespace Alpha.Api.Services.Interfaces;

public interface ISurveyAssignmentService
{
    Task<SurveyAssignmentResponse> AssignSurveyToRelationshipsAsync(Guid surveyId, AssignSurveyRequest request);
    Task<SurveyAssignmentResponse> AssignSurveyRelationshipsFromCsvAsync(Guid surveyId, AssignSurveyCsvRequest request);
    Task<SurveyAssignmentResponse> UnassignSurveyFromRelationshipsAsync(Guid surveyId, UnassignSurveyRequest request);
    Task<List<AssignedRelationshipResponse>> GetAssignedRelationshipsAsync(Guid surveyId, string? search = null, string? relationshipType = null);
    Task<List<AvailableRelationshipResponse>> GetAvailableRelationshipsAsync(Guid surveyId, string? search = null, string? relationshipType = null);
    Task<int> GetAssignmentCountAsync(Guid surveyId);
}

