using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

/// <summary>
/// Service for generating survey reports and analytics
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Calculate scores for a subject based on their survey submissions
    /// Maps rating options to positional scores (0-100%)
    /// </summary>
    /// <param name="subjectId">The subject ID</param>
    /// <param name="surveyId">The survey ID (optional, if null calculates for all surveys)</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Subject report with self-evaluation and evaluator scores</returns>
    Task<SubjectReportResponse?> GetSubjectReportAsync(Guid subjectId, Guid? surveyId, Guid tenantId);

    /// <summary>
    /// Compare self-evaluation scores against evaluator average scores for a subject
    /// </summary>
    /// <param name="subjectId">The subject ID</param>
    /// <param name="surveyId">The survey ID (optional)</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Self vs evaluator comparison report</returns>
    Task<SelfVsEvaluatorComparisonResponse?> GetSelfVsEvaluatorComparisonAsync(Guid subjectId, Guid? surveyId, Guid tenantId);

    /// <summary>
    /// Calculate organization-wide average scores and compare subject performance
    /// </summary>
    /// <param name="subjectId">The subject ID</param>
    /// <param name="surveyId">The survey ID (optional)</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Organization comparison report</returns>
    Task<OrganizationComparisonResponse?> GetOrganizationComparisonAsync(Guid subjectId, Guid? surveyId, Guid tenantId);

    /// <summary>
    /// Get comprehensive report combining all metrics for a subject
    /// </summary>
    /// <param name="subjectId">The subject ID</param>
    /// <param name="surveyId">The survey ID (optional)</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Comprehensive report</returns>
    Task<ComprehensiveReportResponse?> GetComprehensiveReportAsync(Guid subjectId, Guid? surveyId, Guid tenantId);
}


