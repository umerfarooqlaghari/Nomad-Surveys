using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.Entities;

namespace Nomad.Api.Repository;

/// <summary>
/// Repository for computing analytics data for reports
/// </summary>
public class ReportAnalyticsRepository
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<ReportAnalyticsRepository> _logger;

    public ReportAnalyticsRepository(
        NomadSurveysDbContext context,
        ILogger<ReportAnalyticsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a subject has completed their self-assessment for a specific survey
    /// </summary>
    /// <param name="subjectId">The subject's ID</param>
    /// <param name="surveyId">The survey's ID</param>
    /// <param name="tenantId">The tenant's ID</param>
    /// <returns>A string indicating the self-assessment status: "Completed", "In Progress", "Pending", or "Not Assigned"</returns>
    public async Task<string> GetSelfAssessmentStatusAsync(Guid subjectId, Guid surveyId, Guid tenantId)
    {
        try
        {
            _logger.LogInformation(
                "Checking self-assessment status for Subject: {SubjectId}, Survey: {SurveyId}, Tenant: {TenantId}",
                subjectId, surveyId, tenantId);

            // Get the subject with their employee info
            var subject = await _context.Subjects
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.TenantId == tenantId);

            if (subject == null)
            {
                _logger.LogWarning("Subject {SubjectId} not found in tenant {TenantId}", subjectId, tenantId);
                return "Not Found";
            }

            // First, check if self-assessment is assigned via SubjectEvaluatorSurvey
            // Self-assessment is identified by:
            // 1. Relationship type is "Self" (case-insensitive) OR
            // 2. Evaluator's EmployeeId equals Subject's EmployeeId
            var selfAssignment = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Evaluator)
                .Where(ses => ses.SubjectEvaluator.SubjectId == subjectId
                    && ses.SurveyId == surveyId
                    && ses.TenantId == tenantId)
                .Where(ses =>
                    // Check if relationship is "Self" (case-insensitive using ToLower for EF Core translation)
                    // Note: Using ToLower() because string.Equals with StringComparison is not supported by EF Core
                    (ses.SubjectEvaluator.Relationship != null && ses.SubjectEvaluator.Relationship.ToLower() == "self") ||
                    // Or check if evaluator's employee is the same as subject's employee
                    ses.SubjectEvaluator.Evaluator.EmployeeId == subject.EmployeeId)
                .FirstOrDefaultAsync();

            if (selfAssignment == null)
            {
                _logger.LogInformation(
                    "Self-assessment not assigned for Subject: {SubjectId}, Survey: {SurveyId}",
                    subjectId, surveyId);
                return "Not Assigned";
            }

            // Now check if there's a SurveySubmission for this self-assessment
            var selfAssessmentSubmission = await _context.SurveySubmissions
                .Where(ss => ss.SubjectEvaluatorSurveyId == selfAssignment.Id
                    && ss.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (selfAssessmentSubmission == null)
            {
                // Submission record doesn't exist yet - survey hasn't been started
                _logger.LogInformation(
                    "Self-assessment is assigned but no submission record exists for Subject: {SubjectId}, Survey: {SurveyId}",
                    subjectId, surveyId);
                return "Pending";
            }

            // Return the status based on submission status
            var status = selfAssessmentSubmission.Status;

            _logger.LogInformation(
                "Self-assessment status for Subject: {SubjectId}, Survey: {SurveyId} is {Status}",
                subjectId, surveyId, status);

            return status switch
            {
                SurveySubmissionStatus.Completed => "Completed",
                SurveySubmissionStatus.InProgress => "In Progress",
                SurveySubmissionStatus.Pending => "Pending",
                _ => status // Return as-is if unknown status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking self-assessment status for Subject: {SubjectId}, Survey: {SurveyId}",
                subjectId, surveyId);
            throw;
        }
    }

    /// <summary>
    /// Gets completion statistics per relationship type for a subject and survey
    /// </summary>
    /// <param name="subjectId">The subject's ID</param>
    /// <param name="surveyId">The survey's ID</param>
    /// <param name="tenantId">The tenant's ID</param>
    /// <returns>A list of relationship completion stats (excluding "Self")</returns>
    public async Task<List<RelationshipCompletionStats>> GetRelationshipCompletionStatsAsync(
        Guid subjectId, Guid surveyId, Guid tenantId)
    {
        try
        {
            _logger.LogInformation(
                "Getting relationship completion stats for Subject: {SubjectId}, Survey: {SurveyId}, Tenant: {TenantId}",
                subjectId, surveyId, tenantId);

            // Get all SubjectEvaluatorSurveys for this subject and survey (excluding Self)
            var assignments = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                .Where(ses => ses.SubjectEvaluator.SubjectId == subjectId
                    && ses.SurveyId == surveyId
                    && ses.TenantId == tenantId
                    && ses.SubjectEvaluator.Relationship != null
                    && ses.SubjectEvaluator.Relationship.ToLower() != "self")
                .ToListAsync();

            // Get all submissions for these assignments
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await _context.SurveySubmissions
                .Where(ss => assignmentIds.Contains(ss.SubjectEvaluatorSurveyId)
                    && ss.TenantId == tenantId)
                .ToListAsync();

            // Group by relationship type and calculate stats
            var relationshipGroups = assignments
                .GroupBy(a => a.SubjectEvaluator.Relationship ?? "Unknown")
                .Select(g =>
                {
                    var groupAssignmentIds = g.Select(a => a.Id).ToList();
                    var groupSubmissions = submissions
                        .Where(s => groupAssignmentIds.Contains(s.SubjectEvaluatorSurveyId))
                        .ToList();
                    var completedCount = groupSubmissions
                        .Count(s => s.Status == SurveySubmissionStatus.Completed);
                    var total = g.Count();

                    return new RelationshipCompletionStats
                    {
                        RelationshipType = g.Key,
                        Total = total,
                        Completed = completedCount,
                        PercentComplete = total > 0 ? Math.Round((double)completedCount / total * 100, 0) : 0
                    };
                })
                .OrderBy(r => r.RelationshipType)
                .ToList();

            _logger.LogInformation(
                "Found {Count} relationship groups for Subject: {SubjectId}, Survey: {SurveyId}",
                relationshipGroups.Count, subjectId, surveyId);

            return relationshipGroups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting relationship completion stats for Subject: {SubjectId}, Survey: {SurveyId}",
                subjectId, surveyId);
            throw;
        }
    }
}

/// <summary>
/// DTO for relationship completion statistics
/// </summary>
public class RelationshipCompletionStats
{
    public string RelationshipType { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Completed { get; set; }
    public double PercentComplete { get; set; }
}
