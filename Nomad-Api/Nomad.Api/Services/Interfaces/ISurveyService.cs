using Alpha.Api.DTOs.Request;
using Alpha.Api.DTOs.Response;

namespace Alpha.Api.Services.Interfaces;

public interface ISurveyService
{
    Task<List<SurveyListResponse>> GetSurveysAsync(Guid? tenantId = null);
    Task<SurveyResponse?> GetSurveyByIdAsync(Guid surveyId);
    Task<SurveyResponse> CreateSurveyAsync(CreateSurveyRequest request, Guid tenantId);
    Task<SurveyResponse?> UpdateSurveyAsync(Guid surveyId, UpdateSurveyRequest request);
    Task<bool> DeleteSurveyAsync(Guid surveyId);
    Task<bool> SurveyExistsAsync(Guid surveyId, Guid? tenantId = null);
    Task<SurveyAssignmentResponse> AutoAssignSurveyToAllRelationshipsAsync(Guid surveyId, Guid tenantId);
}

