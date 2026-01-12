using Nomad.Api.DTOs.Report;

namespace Nomad.Api.Repository;

public interface IExcelReportRepository
{
    Task<List<RateeAverageReportItem>> GetRateeAverageReportDataAsync(Guid surveyId, Guid tenantId);
    Task<ClusterCompetencyHierarchyResult> GetClusterCompetencyHierarchyAsync(Guid subjectId, Guid surveyId, Guid tenantId);
    Task<List<SubjectWiseHeatMapItem>> GetSubjectWiseHeatMapDataAsync(Guid surveyId, Guid tenantId);
    Task<List<SubjectWiseConsolidatedReportItem>> GetSubjectWiseConsolidatedDataAsync(Guid surveyId, Guid tenantId);
    Task<List<ReportQuestionDefinition>> GetQuestionColumnsAsync(Guid surveyId, Guid tenantId);
    Task<string> GetSurveyAndTenantDetailsAsync(Guid surveyId, Guid tenantId);
}
