using Nomad.Api.DTOs.Report;

namespace Nomad.Api.Repository;

public interface IExcelReportRepository
{
    Task<List<RateeAverageReportItem>> GetRateeAverageReportDataAsync(Guid surveyId, Guid tenantId);
    Task<ClusterCompetencyHierarchyResult> GetClusterCompetencyHierarchyAsync(Guid subjectId, Guid surveyId, Guid tenantId);
}
