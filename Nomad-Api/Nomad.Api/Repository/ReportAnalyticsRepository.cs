using System.Text.Json;
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

    /// <summary>
    /// Gets highest and lowest scored items for a subject's survey, grouped by dimension (cluster)
    /// Highest scores: average >= 3.0
    /// Lowest scores: average < 3.0
    /// </summary>
    public async Task<HighLowScoresResult> GetHighLowScoresAsync(
        Guid subjectId, Guid surveyId, Guid tenantId)
    {
        try
        {
            _logger.LogInformation(
                "Getting high/low scores for Subject: {SubjectId}, Survey: {SurveyId}, Tenant: {TenantId}",
                subjectId, surveyId, tenantId);

            var result = new HighLowScoresResult();

            // Get the subject
            var subject = await _context.Subjects
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.TenantId == tenantId);

            if (subject == null)
            {
                _logger.LogWarning("Subject {SubjectId} not found", subjectId);
                return result;
            }

            // Get the survey with schema
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && s.TenantId == tenantId);

            if (survey == null)
            {
                _logger.LogWarning("Survey {SurveyId} not found", surveyId);
                return result;
            }

            // Get all completed submissions for this subject (EXCLUDING self-assessment)
            // Self-assessment is identified by:
            // 1. Relationship type is "Self" (case-insensitive) OR
            // 2. Evaluator's EmployeeId equals Subject's EmployeeId
            var submissions = await _context.SurveySubmissions
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                        .ThenInclude(se => se.Evaluator)
                .Where(ss => ss.SubjectId == subjectId
                    && ss.SurveyId == surveyId
                    && ss.TenantId == tenantId
                    && ss.Status == SurveySubmissionStatus.Completed
                    && ss.ResponseData != null
                    && ss.SubjectEvaluatorSurvey != null
                    && !(
                        // Exclude self: relationship is "Self" OR evaluator's employee matches subject's employee
                        (ss.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship != null
                         && ss.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship.ToLower() == "self") ||
                        ss.SubjectEvaluatorSurvey.SubjectEvaluator.Evaluator.EmployeeId == subject.EmployeeId
                    ))
                .ToListAsync();

            _logger.LogInformation(
                "Found {TotalSubmissions} evaluator submissions (excluding self) for Subject: {SubjectId}, Survey: {SurveyId}",
                submissions.Count, subjectId, surveyId);

            if (!submissions.Any())
            {
                _logger.LogInformation("No completed submissions found for Subject: {SubjectId}", subjectId);
                return result;
            }

            // Extract questions with cluster/competency info from survey schema
            var questionMappings = await ExtractQuestionMappings(survey.Schema, tenantId);

            _logger.LogInformation(
                "Extracted {QuestionCount} question mappings from survey schema",
                questionMappings.Count);

            // Calculate average score per question across all submissions
            var questionScores = CalculateQuestionAverages(submissions, questionMappings);

            _logger.LogInformation(
                "Calculated scores for {ScoreCount} questions",
                questionScores.Count);

            // Separate into highest (>= 3) and lowest (< 3) scores
            result.HighestScores = questionScores
                .Where(qs => qs.AverageScore >= 3.0)
                .OrderByDescending(qs => qs.AverageScore)
                .Take(10) // Limit to top 10
                .Select((qs, index) => new HighLowScoreItem
                {
                    Rank = index + 1,
                    Dimension = qs.ClusterName,
                    Item = qs.QuestionText,
                    Average = Math.Round(qs.AverageScore, 2)
                })
                .ToList();

            result.LowestScores = questionScores
                .Where(qs => qs.AverageScore < 3.0)
                .OrderBy(qs => qs.AverageScore)
                .Take(10) // Limit to top 10
                .Select((qs, index) => new HighLowScoreItem
                {
                    Rank = index + 1,
                    Dimension = qs.ClusterName,
                    Item = qs.QuestionText,
                    Average = Math.Round(qs.AverageScore, 2)
                })
                .ToList();

            _logger.LogInformation(
                "Found {HighCount} highest scores and {LowCount} lowest scores for Subject: {SubjectId}",
                result.HighestScores.Count, result.LowestScores.Count, subjectId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting high/low scores for Subject: {SubjectId}, Survey: {SurveyId}",
                subjectId, surveyId);
            throw;
        }
    }

    /// <summary>
    /// Gets latent strengths and blindspots by comparing self scores vs others scores per question.
    /// Latent Strengths: Gap (Others - Self) is positive (others rated higher than self)
    /// Blindspots: Gap (Others - Self) is negative (others rated lower than self)
    /// </summary>
    public async Task<LatentStrengthsBlindspotsResult> GetLatentStrengthsAndBlindspotsAsync(
        Guid subjectId, Guid surveyId, Guid tenantId)
    {
        try
        {
            _logger.LogInformation(
                "Getting latent strengths/blindspots for Subject: {SubjectId}, Survey: {SurveyId}, Tenant: {TenantId}",
                subjectId, surveyId, tenantId);

            var result = new LatentStrengthsBlindspotsResult();

            // Get the subject with employee info
            var subject = await _context.Subjects
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.TenantId == tenantId);

            if (subject == null)
            {
                _logger.LogWarning("Subject {SubjectId} not found", subjectId);
                return result;
            }

            // Get the survey with schema
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && s.TenantId == tenantId);

            if (survey == null)
            {
                _logger.LogWarning("Survey {SurveyId} not found", surveyId);
                return result;
            }

            // Get self-assessment submission
            // Self-assessment is identified by:
            // 1. Relationship type is "Self" (case-insensitive) OR
            // 2. Evaluator's EmployeeId equals Subject's EmployeeId
            var selfSubmission = await _context.SurveySubmissions
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                        .ThenInclude(se => se.Evaluator)
                .Where(ss => ss.SubjectId == subjectId
                    && ss.SurveyId == surveyId
                    && ss.TenantId == tenantId
                    && ss.Status == SurveySubmissionStatus.Completed
                    && ss.ResponseData != null
                    && ss.SubjectEvaluatorSurvey != null
                    && (
                        // Check if relationship is "Self" (case-insensitive)
                        (ss.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship != null
                         && ss.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship.ToLower() == "self") ||
                        // Or check if evaluator's employee is the same as subject's employee
                        ss.SubjectEvaluatorSurvey.SubjectEvaluator.Evaluator.EmployeeId == subject.EmployeeId
                    ))
                .FirstOrDefaultAsync();

            // Get all evaluator submissions (excluding self)
            var evaluatorSubmissions = await _context.SurveySubmissions
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                        .ThenInclude(se => se.Evaluator)
                .Where(ss => ss.SubjectId == subjectId
                    && ss.SurveyId == surveyId
                    && ss.TenantId == tenantId
                    && ss.Status == SurveySubmissionStatus.Completed
                    && ss.ResponseData != null
                    && ss.SubjectEvaluatorSurvey != null
                    && !(
                        // Exclude self: relationship is "Self" OR evaluator's employee matches subject's employee
                        (ss.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship != null
                         && ss.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship.ToLower() == "self") ||
                        ss.SubjectEvaluatorSurvey.SubjectEvaluator.Evaluator.EmployeeId == subject.EmployeeId
                    ))
                .ToListAsync();

            _logger.LogInformation(
                "Found self-assessment: {HasSelf}, Evaluator submissions: {EvaluatorCount}",
                selfSubmission != null, evaluatorSubmissions.Count);

            if (selfSubmission == null || !evaluatorSubmissions.Any())
            {
                _logger.LogInformation("Missing self-assessment or evaluator submissions");
                return result;
            }

            // Extract question mappings from survey schema
            var questionMappings = await ExtractQuestionMappings(survey.Schema, tenantId);

            if (questionMappings.Count == 0)
            {
                _logger.LogWarning("No question mappings found in survey schema");
                return result;
            }

            // Calculate self scores per question
            var selfScores = CalculateScoresForSubmission(selfSubmission, questionMappings);

            // Calculate average others scores per question
            var othersScores = CalculateAverageScoresPerQuestion(evaluatorSubmissions, questionMappings);

            _logger.LogInformation("Self scores: {SelfCount}, Others scores: {OthersCount}",
                selfScores.Count, othersScores.Count);

            // Calculate gaps and categorize
            var gapItems = new List<GapScoreItem>();

            foreach (var questionId in questionMappings.Keys)
            {
                if (!selfScores.TryGetValue(questionId, out var selfScore))
                    continue;
                if (!othersScores.TryGetValue(questionId, out var othersScore))
                    continue;

                var mapping = questionMappings[questionId];
                var gap = Math.Round(othersScore - selfScore, 2);

                gapItems.Add(new GapScoreItem
                {
                    ScoringCategory = mapping.ClusterName,
                    Item = mapping.QuestionText,
                    Self = Math.Round(selfScore, 2),
                    Others = Math.Round(othersScore, 2),
                    Gap = gap
                });
            }

            // Latent Strengths: positive gap (others rated higher), ordered by gap descending
            result.LatentStrengths = gapItems
                .Where(g => g.Gap > 0)
                .OrderByDescending(g => g.Gap)
                .Take(10)
                .Select((g, index) => new GapScoreItem
                {
                    Rank = index + 1,
                    ScoringCategory = g.ScoringCategory,
                    Item = g.Item,
                    Self = g.Self,
                    Others = g.Others,
                    Gap = g.Gap
                })
                .ToList();

            // Blindspots: negative gap (others rated lower), ordered by gap ascending (most negative first)
            result.Blindspots = gapItems
                .Where(g => g.Gap < 0)
                .OrderBy(g => g.Gap)
                .Take(10)
                .Select((g, index) => new GapScoreItem
                {
                    Rank = index + 1,
                    ScoringCategory = g.ScoringCategory,
                    Item = g.Item,
                    Self = g.Self,
                    Others = g.Others,
                    Gap = g.Gap
                })
                .ToList();

            _logger.LogInformation(
                "Found {LatentCount} latent strengths and {BlindCount} blindspots",
                result.LatentStrengths.Count, result.Blindspots.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting latent strengths/blindspots for Subject: {SubjectId}, Survey: {SurveyId}",
                subjectId, surveyId);
            throw;
        }
    }

    /// <summary>
    /// Calculates scores for a single submission (used for self-assessment)
    /// </summary>
    private Dictionary<string, double> CalculateScoresForSubmission(
        SurveySubmission submission,
        Dictionary<string, QuestionMapping> questionMappings)
    {
        var scores = new Dictionary<string, double>();

        if (submission.ResponseData == null)
            return scores;

        try
        {
            var responseRoot = submission.ResponseData.RootElement;

            foreach (var mapping in questionMappings)
            {
                var questionId = mapping.Key;
                var questionInfo = mapping.Value;

                if (!responseRoot.TryGetProperty(questionId, out var answerElement))
                    continue;

                double? answerValue = null;

                if (answerElement.ValueKind == JsonValueKind.Number)
                {
                    answerValue = answerElement.GetDouble();
                }
                else if (answerElement.ValueKind == JsonValueKind.String)
                {
                    var answerText = answerElement.GetString();

                    if (double.TryParse(answerText, out var parsed))
                    {
                        answerValue = parsed;
                    }
                    else if (!string.IsNullOrEmpty(answerText) &&
                             questionInfo.RatingOptionsMap.TryGetValue(answerText, out var mappedValue))
                    {
                        answerValue = mappedValue;
                    }
                }

                if (answerValue.HasValue)
                {
                    scores[questionId] = answerValue.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating scores for submission {Id}", submission.Id);
        }

        return scores;
    }

    /// <summary>
    /// Calculates average scores per question across multiple submissions
    /// </summary>
    private Dictionary<string, double> CalculateAverageScoresPerQuestion(
        List<SurveySubmission> submissions,
        Dictionary<string, QuestionMapping> questionMappings)
    {
        var scoresAccumulator = new Dictionary<string, List<double>>();

        foreach (var submission in submissions)
        {
            var submissionScores = CalculateScoresForSubmission(submission, questionMappings);

            foreach (var kvp in submissionScores)
            {
                if (!scoresAccumulator.ContainsKey(kvp.Key))
                    scoresAccumulator[kvp.Key] = new List<double>();

                scoresAccumulator[kvp.Key].Add(kvp.Value);
            }
        }

        return scoresAccumulator.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Average()
        );
    }

    /// <summary>
    /// Extracts question mappings from survey schema including cluster/competency info
    /// </summary>
    private async Task<Dictionary<string, QuestionMapping>> ExtractQuestionMappings(JsonDocument schema, Guid tenantId)
    {
        var mappings = new Dictionary<string, QuestionMapping>();

        try
        {
            var root = schema.RootElement;

            if (!root.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array)
                return mappings;

            // Pre-load all clusters with competencies for this tenant
            var clusters = await _context.Clusters
                .Include(c => c.Competencies)
                .Where(c => c.TenantId == tenantId)
                .ToDictionaryAsync(c => c.Id, c => c);

            // Pre-load all competencies for this tenant
            var competencies = await _context.Competencies
                .Where(c => c.TenantId == tenantId)
                .ToDictionaryAsync(c => c.Id, c => c);

            foreach (var page in pages.EnumerateArray())
            {
                JsonElement questions;
                if (!page.TryGetProperty("questions", out questions))
                    if (!page.TryGetProperty("elements", out questions))
                        continue;

                if (questions.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var question in questions.EnumerateArray())
                {
                    var questionType = question.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
                    var questionId = question.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    var questionName = question.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;

                    _logger.LogDebug("Found question: id={Id}, name={Name}, type={Type}", questionId, questionName, questionType);

                    // Only process rating-based questions (rating, radiogroup with rating options)
                    var isRatingType = questionType == "rating" || questionType == "radiogroup";
                    if (!isRatingType)
                        continue;

                    // Use id as the key since that's what response data uses
                    if (string.IsNullOrEmpty(questionId))
                        continue;

                    // Get question text (prefer othersText for evaluator display, fallback to selfText or title)
                    var questionText = question.TryGetProperty("othersText", out var othersTextEl) ? othersTextEl.GetString() : null;
                    if (string.IsNullOrEmpty(questionText))
                        questionText = question.TryGetProperty("selfText", out var selfTextEl) ? selfTextEl.GetString() : null;
                    if (string.IsNullOrEmpty(questionText))
                        questionText = question.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;

                    // Extract cluster info from importedFrom metadata
                    string? clusterName = null;
                    string? competencyName = null;

                    if (question.TryGetProperty("importedFrom", out var importedFrom))
                    {
                        _logger.LogInformation("Question {QuestionName} has importedFrom metadata", questionName);

                        if (importedFrom.TryGetProperty("clusterId", out var clusterIdEl))
                        {
                            var clusterIdStr = clusterIdEl.GetString();
                            _logger.LogInformation("  clusterId from schema: '{ClusterId}'", clusterIdStr ?? "(null)");
                            if (!string.IsNullOrEmpty(clusterIdStr) && Guid.TryParse(clusterIdStr, out var clusterId) && clusters.TryGetValue(clusterId, out var cluster))
                            {
                                clusterName = cluster.ClusterName;
                                _logger.LogInformation("  Found cluster directly: {ClusterName}", clusterName);
                            }
                        }

                        if (importedFrom.TryGetProperty("competencyId", out var competencyIdEl))
                        {
                            var competencyIdStr = competencyIdEl.GetString();
                            _logger.LogInformation("  competencyId from schema: '{CompetencyId}'", competencyIdStr ?? "(null)");
                            if (!string.IsNullOrEmpty(competencyIdStr) && Guid.TryParse(competencyIdStr, out var competencyId))
                            {
                                _logger.LogInformation("  Parsed competencyId: {CompetencyId}", competencyId);
                                if (competencies.TryGetValue(competencyId, out var competency))
                                {
                                    competencyName = competency.Name;
                                    _logger.LogInformation("  Found competency: {CompetencyName}, ClusterId: {ClusterId}",
                                        competencyName, competency.ClusterId);
                                    // If we got competency but not cluster, get cluster from competency
                                    if (string.IsNullOrEmpty(clusterName))
                                    {
                                        if (clusters.TryGetValue(competency.ClusterId, out var parentCluster))
                                        {
                                            clusterName = parentCluster.ClusterName;
                                            _logger.LogInformation("  Got cluster from competency: {ClusterName}", clusterName);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("  Cluster {ClusterId} not found in clusters dictionary (count: {Count})",
                                                competency.ClusterId, clusters.Count);
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("  Competency {CompetencyId} not found in competencies dictionary (count: {Count})",
                                        competencyId, competencies.Count);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("  Failed to parse competencyId: '{CompetencyIdStr}'", competencyIdStr);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Question {QuestionName} has NO importedFrom metadata", questionName);
                    }

                    // Get rating scale and options from config
                    var ratingMin = 1;
                    var ratingMax = 5;
                    var ratingOptionsMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    if (question.TryGetProperty("config", out var config))
                    {
                        if (config.TryGetProperty("ratingMin", out var minEl)) ratingMin = minEl.GetInt32();
                        if (config.TryGetProperty("ratingMax", out var maxEl)) ratingMax = maxEl.GetInt32();

                        // Extract rating options to map text to numeric value
                        if (config.TryGetProperty("ratingOptions", out var ratingOptions) &&
                            ratingOptions.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var option in ratingOptions.EnumerateArray())
                            {
                                var optionText = option.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
                                var optionId = option.TryGetProperty("id", out var optIdEl) ? optIdEl.GetString() : null;

                                if (!string.IsNullOrEmpty(optionText) && !string.IsNullOrEmpty(optionId) &&
                                    int.TryParse(optionId, out var numericValue))
                                {
                                    ratingOptionsMap[optionText] = numericValue;
                                }
                            }
                        }
                    }

                    _logger.LogInformation("Question {Id} rating options: {Options}",
                        questionId, string.Join(", ", ratingOptionsMap.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                    mappings[questionId] = new QuestionMapping
                    {
                        QuestionName = questionId,
                        QuestionText = questionText ?? questionName ?? questionId,
                        ClusterName = clusterName ?? "Uncategorized",
                        CompetencyName = competencyName ?? "General",
                        RatingMin = ratingMin,
                        RatingMax = ratingMax,
                        RatingOptionsMap = ratingOptionsMap
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting question mappings from schema");
        }

        return mappings;
    }

    /// <summary>
    /// Calculates average scores per question across all evaluator submissions
    /// </summary>
    private List<QuestionScoreResult> CalculateQuestionAverages(
        List<SurveySubmission> submissions,
        Dictionary<string, QuestionMapping> questionMappings)
    {
        var questionScoresAccumulator = new Dictionary<string, List<double>>();

        _logger.LogInformation("Processing {SubmissionCount} submissions against {MappingCount} question mappings",
            submissions.Count, questionMappings.Count);

        // Log the question names we're looking for (once)
        _logger.LogInformation("Looking for question names: {Names}",
            string.Join(", ", questionMappings.Keys));

        foreach (var submission in submissions)
        {
            if (submission.ResponseData == null)
            {
                _logger.LogInformation("Submission {Id} has null ResponseData", submission.Id);
                continue;
            }

            try
            {
                var responseRoot = submission.ResponseData.RootElement;

                // Log available keys in response data for debugging (only for first submission)
                if (responseRoot.ValueKind == JsonValueKind.Object)
                {
                    var keys = responseRoot.EnumerateObject().Select(p => p.Name).ToList();
                    _logger.LogInformation("Submission {Id} has response keys: {Keys}",
                        submission.Id, string.Join(", ", keys));
                }

                foreach (var mapping in questionMappings)
                {
                    var questionId = mapping.Key;
                    var questionInfo = mapping.Value;

                    if (!responseRoot.TryGetProperty(questionId, out var answerElement))
                    {
                        continue;
                    }

                    double? answerValue = null;

                    // Try to parse the answer value
                    if (answerElement.ValueKind == JsonValueKind.Number)
                    {
                        answerValue = answerElement.GetDouble();
                    }
                    else if (answerElement.ValueKind == JsonValueKind.String)
                    {
                        var answerText = answerElement.GetString();

                        // First try to parse as a number
                        if (double.TryParse(answerText, out var parsed))
                        {
                            answerValue = parsed;
                        }
                        // If not a number, look up in rating options map
                        else if (!string.IsNullOrEmpty(answerText) &&
                                 questionInfo.RatingOptionsMap.TryGetValue(answerText, out var mappedValue))
                        {
                            answerValue = mappedValue;
                            _logger.LogInformation("  Question {Id}: mapped '{Text}' to {Value}",
                                questionId, answerText, mappedValue);
                        }
                        else
                        {
                            _logger.LogWarning("  Question {Id}: could not map answer '{Text}'",
                                questionId, answerText);
                        }
                    }

                    if (!answerValue.HasValue)
                        continue;

                    if (!questionScoresAccumulator.ContainsKey(questionId))
                        questionScoresAccumulator[questionId] = new List<double>();

                    questionScoresAccumulator[questionId].Add(answerValue.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing response data for submission {SubmissionId}", submission.Id);
            }
        }

        // Calculate averages
        var results = new List<QuestionScoreResult>();
        foreach (var kvp in questionScoresAccumulator)
        {
            if (!kvp.Value.Any() || !questionMappings.TryGetValue(kvp.Key, out var mapping))
                continue;

            results.Add(new QuestionScoreResult
            {
                QuestionName = kvp.Key,
                QuestionText = mapping.QuestionText,
                ClusterName = mapping.ClusterName,
                CompetencyName = mapping.CompetencyName,
                AverageScore = kvp.Value.Average()
            });
        }

        return results;
    }

    private class QuestionMapping
    {
        public string QuestionName { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string ClusterName { get; set; } = string.Empty;
        public string CompetencyName { get; set; } = string.Empty;
        public int RatingMin { get; set; } = 1;
        public int RatingMax { get; set; } = 5;
        /// <summary>
        /// Maps rating option text (e.g., "Satisfied") to numeric value (e.g., 4)
        /// </summary>
        public Dictionary<string, int> RatingOptionsMap { get; set; } = new();
    }

    private class QuestionScoreResult
    {
        public string QuestionName { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string ClusterName { get; set; } = string.Empty;
        public string CompetencyName { get; set; } = string.Empty;
        public double AverageScore { get; set; }
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

/// <summary>
/// Result DTO for high/low scores
/// </summary>
public class HighLowScoresResult
{
    public List<HighLowScoreItem> HighestScores { get; set; } = new();
    public List<HighLowScoreItem> LowestScores { get; set; } = new();
}

/// <summary>
/// Individual high/low score item
/// </summary>
public class HighLowScoreItem
{
    public int Rank { get; set; }
    public string Dimension { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public double Average { get; set; }
}

/// <summary>
/// Result DTO for latent strengths and blindspots (gap analysis)
/// </summary>
public class LatentStrengthsBlindspotsResult
{
    public List<GapScoreItem> LatentStrengths { get; set; } = new();
    public List<GapScoreItem> Blindspots { get; set; } = new();
}

/// <summary>
/// Individual gap score item for latent strengths/blindspots
/// </summary>
public class GapScoreItem
{
    public int Rank { get; set; }
    public string ScoringCategory { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public double Self { get; set; }
    public double Others { get; set; }
    public double Gap { get; set; }
}
