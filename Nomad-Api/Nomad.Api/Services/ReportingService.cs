using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using System.Text.Json;

namespace Nomad.Api.Services;

/// <summary>
/// Service for generating survey reports and analytics
/// </summary>
public class ReportingService : IReportingService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        NomadSurveysDbContext context,
        ILogger<ReportingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SubjectReportResponse?> GetSubjectReportAsync(Guid subjectId, Guid? surveyId, Guid tenantId)
    {
        try
        {
            // Get subject with employee
            var subject = await _context.Subjects
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.TenantId == tenantId);

            if (subject == null)
            {
                _logger.LogWarning("Subject {SubjectId} not found for tenant {TenantId}", subjectId, tenantId);
                return null;
            }

            // Build query for survey submissions
            var submissionsQuery = _context.SurveySubmissions
                .Include(ss => ss.Survey)
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                        .ThenInclude(se => se.Evaluator)
                            .ThenInclude(e => e.Employee)
                .Where(ss => ss.SubjectId == subjectId 
                          && ss.Status == SurveySubmissionStatus.Completed
                          && ss.TenantId == tenantId);

            if (surveyId.HasValue)
            {
                submissionsQuery = submissionsQuery.Where(ss => ss.SurveyId == surveyId.Value);
            }

            var submissions = await submissionsQuery.ToListAsync();

            if (!submissions.Any())
            {
                _logger.LogWarning("No completed submissions found for subject {SubjectId}", subjectId);
                return null;
            }

            // Get first survey (assuming all are from same survey if surveyId is provided)
            var survey = submissions.First().Survey;

            // Parse survey schema to extract rating questions
            var ratingQuestions = ExtractRatingQuestions(survey.Schema);

            // Separate self-evaluations from evaluator responses
            var selfEvaluations = new List<SurveySubmission>();
            var evaluatorSubmissions = new List<SurveySubmission>();

            foreach (var submission in submissions)
            {
                // Check if this is a self-evaluation
                // 1. Check if evaluator's employee == subject's employee
                // 2. Check if relationship type is "Self"
                var evaluatorEmployeeId = submission.SubjectEvaluatorSurvey.SubjectEvaluator.Evaluator.EmployeeId;
                var subjectEmployeeId = subject.EmployeeId;
                var relationship = submission.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship;

                bool isSelfEvaluation = evaluatorEmployeeId == subjectEmployeeId 
                    || string.Equals(relationship, "Self", StringComparison.OrdinalIgnoreCase);

                if (isSelfEvaluation)
                {
                    selfEvaluations.Add(submission);
                }
                else
                {
                    evaluatorSubmissions.Add(submission);
                }
            }

            // Calculate scores
            var selfEvaluationScores = selfEvaluations.Any()
                ? CalculateScores(selfEvaluations.First().ResponseData, ratingQuestions)
                : null;

            var evaluatorAverageScores = evaluatorSubmissions.Any()
                ? CalculateAverageScores(evaluatorSubmissions.Select(s => s.ResponseData).ToList(), ratingQuestions)
                : null;

            return new SubjectReportResponse
            {
                SubjectId = subjectId,
                SubjectName = subject.FullName,
                SurveyId = survey.Id,
                SurveyTitle = survey.Title,
                SelfEvaluation = selfEvaluationScores,
                EvaluatorAverage = evaluatorAverageScores
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating subject report for {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<SelfVsEvaluatorComparisonResponse?> GetSelfVsEvaluatorComparisonAsync(Guid subjectId, Guid? surveyId, Guid tenantId)
    {
        try
        {
            var report = await GetSubjectReportAsync(subjectId, surveyId, tenantId);
            if (report == null || report.SelfEvaluation == null || report.EvaluatorAverage == null)
            {
                return null;
            }

            // Get evaluator count
            var submissionsQuery = _context.SurveySubmissions
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                        .ThenInclude(se => se.Evaluator)
                .Where(ss => ss.SubjectId == subjectId 
                          && ss.Status == SurveySubmissionStatus.Completed
                          && ss.TenantId == tenantId);

            if (surveyId.HasValue)
            {
                submissionsQuery = submissionsQuery.Where(ss => ss.SurveyId == surveyId.Value);
            }

            var submissions = await submissionsQuery.ToListAsync();
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.TenantId == tenantId);

            var evaluatorCount = submissions.Count(s =>
            {
                var evaluatorEmployeeId = s.SubjectEvaluatorSurvey.SubjectEvaluator.Evaluator.EmployeeId;
                var relationship = s.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship;
                bool isSelfEvaluation = evaluatorEmployeeId == subject.EmployeeId 
                    || string.Equals(relationship, "Self", StringComparison.OrdinalIgnoreCase);
                return !isSelfEvaluation;
            });

            // Calculate overall differences
            var selfScore = report.SelfEvaluation.OverallScore;
            var evaluatorScore = report.EvaluatorAverage.OverallScore;
            var difference = selfScore - evaluatorScore;
            var percentageDifference = evaluatorScore > 0 ? (difference / evaluatorScore) * 100 : 0;

            // Question-level comparisons
            var questionComparisons = new List<QuestionComparisonDto>();
            foreach (var selfQuestion in report.SelfEvaluation.QuestionScores)
            {
                var evaluatorQuestion = report.EvaluatorAverage.QuestionScores
                    .FirstOrDefault(q => q.QuestionId == selfQuestion.QuestionId);

                if (evaluatorQuestion != null)
                {
                    questionComparisons.Add(new QuestionComparisonDto
                    {
                        QuestionId = selfQuestion.QuestionId,
                        QuestionName = selfQuestion.QuestionName,
                        QuestionText = selfQuestion.QuestionText,
                        SelfScore = selfQuestion.Score,
                        EvaluatorAverageScore = evaluatorQuestion.Score,
                        ScoreDifference = selfQuestion.Score - evaluatorQuestion.Score
                    });
                }
            }

            return new SelfVsEvaluatorComparisonResponse
            {
                SubjectId = subjectId,
                SubjectName = report.SubjectName,
                SurveyId = report.SurveyId,
                SurveyTitle = report.SurveyTitle,
                SelfEvaluationScore = selfScore,
                EvaluatorAverageScore = evaluatorScore,
                ScoreDifference = difference,
                PercentageDifference = percentageDifference,
                QuestionComparisons = questionComparisons,
                EvaluatorCount = evaluatorCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating self vs evaluator comparison for {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<OrganizationComparisonResponse?> GetOrganizationComparisonAsync(Guid subjectId, Guid? surveyId, Guid tenantId)
    {
        try
        {
            // Get subject report for subject scores
            var subjectReport = await GetSubjectReportAsync(subjectId, surveyId, tenantId);
            if (subjectReport == null || subjectReport.EvaluatorAverage == null)
            {
                return null;
            }

            var subjectOverallScore = subjectReport.EvaluatorAverage.OverallScore; // Use evaluator average for comparison

            // Get all completed submissions for the organization (same survey, same tenant)
            var organizationSubmissionsQuery = _context.SurveySubmissions
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(ss => ss.Survey)
                .Where(ss => ss.Status == SurveySubmissionStatus.Completed
                          && ss.TenantId == tenantId);

            if (surveyId.HasValue)
            {
                organizationSubmissionsQuery = organizationSubmissionsQuery.Where(ss => ss.SurveyId == surveyId.Value);
            }

            var organizationSubmissions = await organizationSubmissionsQuery.ToListAsync();

            if (!organizationSubmissions.Any())
            {
                return null;
            }

            var survey = organizationSubmissions.First().Survey;
            var ratingQuestions = ExtractRatingQuestions(survey.Schema);

            // Group submissions by subject and exclude self-evaluations
            var subjectGroups = organizationSubmissions
                .GroupBy(ss => ss.SubjectId)
                .ToList();

            var subjectScores = new List<double>();
            var subjectQuestionScores = new Dictionary<string, List<double>>();

            // Initialize question score lists
            foreach (var question in ratingQuestions)
            {
                subjectQuestionScores[question.Key] = new List<double>();
            }

            // Calculate average scores for each subject (using only evaluator submissions)
            foreach (var subjectGroup in subjectGroups)
            {
                var subject = subjectGroup.First().Subject;
                var subjectEmployeeId = subject.EmployeeId;

                // Get only evaluator submissions (exclude self-evaluations)
                var evaluatorSubmissionsForSubject = subjectGroup
                    .Where(ss =>
                    {
                        var evaluatorEmployeeId = ss.SubjectEvaluatorSurvey.SubjectEvaluator.Evaluator.EmployeeId;
                        var relationship = ss.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship;
                        bool isSelfEvaluation = evaluatorEmployeeId == subjectEmployeeId 
                            || string.Equals(relationship, "Self", StringComparison.OrdinalIgnoreCase);
                        return !isSelfEvaluation;
                    })
                    .ToList();

                if (!evaluatorSubmissionsForSubject.Any())
                    continue;

                // Calculate average scores for this subject
                var avgScores = CalculateAverageScores(
                    evaluatorSubmissionsForSubject.Select(s => s.ResponseData).ToList(),
                    ratingQuestions);

                if (avgScores != null)
                {
                    subjectScores.Add(avgScores.OverallScore);

                    foreach (var questionScore in avgScores.QuestionScores)
                    {
                        if (subjectQuestionScores.ContainsKey(questionScore.QuestionId))
                        {
                            subjectQuestionScores[questionScore.QuestionId].Add(questionScore.Score);
                        }
                    }
                }
            }

            if (!subjectScores.Any())
            {
                return null;
            }

            // Calculate organization averages
            var organizationAverageScore = subjectScores.Average();
            var scoreDifference = subjectOverallScore - organizationAverageScore;
            var percentageDifference = organizationAverageScore > 0 ? (scoreDifference / organizationAverageScore) * 100 : 0;

            // Determine performance level
            var performanceLevel = PerformanceLevel.AtPar;
            if (Math.Abs(scoreDifference) < 0.01) // Within 0.01% difference
            {
                performanceLevel = PerformanceLevel.AtPar;
            }
            else if (scoreDifference > 0)
            {
                performanceLevel = PerformanceLevel.AbovePar;
            }
            else
            {
                performanceLevel = PerformanceLevel.BelowPar;
            }

            // Question-level comparisons
            var questionComparisons = new List<QuestionComparisonDto>();
            foreach (var subjectQuestion in subjectReport.EvaluatorAverage.QuestionScores)
            {
                if (subjectQuestionScores.ContainsKey(subjectQuestion.QuestionId) 
                    && subjectQuestionScores[subjectQuestion.QuestionId].Any())
                {
                    var orgAvgForQuestion = subjectQuestionScores[subjectQuestion.QuestionId].Average();
                    questionComparisons.Add(new QuestionComparisonDto
                    {
                        QuestionId = subjectQuestion.QuestionId,
                        QuestionName = subjectQuestion.QuestionName,
                        QuestionText = subjectQuestion.QuestionText,
                        SelfScore = subjectQuestion.Score,
                        EvaluatorAverageScore = orgAvgForQuestion,
                        ScoreDifference = subjectQuestion.Score - orgAvgForQuestion
                    });
                }
            }

            return new OrganizationComparisonResponse
            {
                SubjectId = subjectId,
                SubjectName = subjectReport.SubjectName,
                SurveyId = subjectReport.SurveyId,
                SurveyTitle = subjectReport.SurveyTitle,
                SubjectOverallScore = subjectOverallScore,
                OrganizationAverageScore = organizationAverageScore,
                ScoreDifference = scoreDifference,
                PercentageDifference = percentageDifference,
                PerformanceLevel = performanceLevel,
                TotalSubjectsInOrg = subjectGroups.Count,
                QuestionComparisons = questionComparisons
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating organization comparison for {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<ComprehensiveReportResponse?> GetComprehensiveReportAsync(Guid subjectId, Guid? surveyId, Guid tenantId)
    {
        try
        {
            var subjectReport = await GetSubjectReportAsync(subjectId, surveyId, tenantId);
            if (subjectReport == null)
            {
                return null;
            }

            var selfVsEvaluatorComparison = await GetSelfVsEvaluatorComparisonAsync(subjectId, surveyId, tenantId);
            var orgComparison = await GetOrganizationComparisonAsync(subjectId, surveyId, tenantId);

            return new ComprehensiveReportResponse
            {
                SubjectId = subjectId,
                SubjectName = subjectReport.SubjectName,
                SurveyId = subjectReport.SurveyId,
                SurveyTitle = subjectReport.SurveyTitle,
                SelfEvaluation = subjectReport.SelfEvaluation,
                EvaluatorAverage = subjectReport.EvaluatorAverage,
                EvaluatorCount = selfVsEvaluatorComparison?.EvaluatorCount ?? 0,
                SelfVsEvaluatorDifference = selfVsEvaluatorComparison?.ScoreDifference ?? 0,
                SelfVsEvaluatorPercentageDifference = selfVsEvaluatorComparison?.PercentageDifference ?? 0,
                OrganizationAverageScore = orgComparison?.OrganizationAverageScore ?? 0,
                SubjectVsOrganizationDifference = orgComparison?.ScoreDifference ?? 0,
                SubjectVsOrganizationPercentageDifference = orgComparison?.PercentageDifference ?? 0,
                PerformanceLevel = orgComparison?.PerformanceLevel ?? PerformanceLevel.AtPar,
                TotalSubjectsInOrg = orgComparison?.TotalSubjectsInOrg ?? 0,
                SelfVsEvaluatorQuestions = selfVsEvaluatorComparison?.QuestionComparisons ?? new List<QuestionComparisonDto>(),
                SubjectVsOrganizationQuestions = orgComparison?.QuestionComparisons ?? new List<QuestionComparisonDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comprehensive report for {SubjectId}", subjectId);
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Extract rating questions from survey schema
    /// Supports both custom schema format (pages/questions) and SurveyJS format (pages/elements)
    /// </summary>
    private Dictionary<string, RatingQuestionInfo> ExtractRatingQuestions(JsonDocument schema)
    {
        var ratingQuestions = new Dictionary<string, RatingQuestionInfo>();

        try
        {
            var root = schema.RootElement;

            if (root.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
            {
                foreach (var page in pages.EnumerateArray())
                {
                    // Try custom format: pages[].questions[]
                    if (page.TryGetProperty("questions", out var questions) && questions.ValueKind == JsonValueKind.Array)
                    {
                        ExtractQuestionsFromArray(questions, ratingQuestions);
                    }
                    // Try SurveyJS format: pages[].elements[]
                    else if (page.TryGetProperty("elements", out var elements) && elements.ValueKind == JsonValueKind.Array)
                    {
                        ExtractQuestionsFromArray(elements, ratingQuestions);
                    }
                }
            }
            // Try root-level elements (legacy format)
            else if (root.TryGetProperty("elements", out var rootElements) && rootElements.ValueKind == JsonValueKind.Array)
            {
                ExtractQuestionsFromArray(rootElements, ratingQuestions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting rating questions from schema");
        }

        return ratingQuestions;
    }

    private void ExtractQuestionsFromArray(JsonElement questionsArray, Dictionary<string, RatingQuestionInfo> ratingQuestions)
    {
        foreach (var questionElement in questionsArray.EnumerateArray())
        {
            // Check if it's a rating question
            var questionType = questionElement.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : null;

            if (questionType != "rating")
                continue;

            // Get question identifier (name or id)
            var questionId = questionElement.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : questionElement.TryGetProperty("id", out var idElement)
                    ? idElement.GetString()
                    : null;

            if (string.IsNullOrEmpty(questionId))
                continue;

            // Get question text
            var questionText = questionElement.TryGetProperty("title", out var titleElement)
                ? titleElement.GetString() ?? string.Empty
                : string.Empty;

            // Extract rating options
            var ratingInfo = new RatingQuestionInfo
            {
                QuestionId = questionId,
                QuestionName = questionElement.TryGetProperty("name", out var nElement) ? nElement.GetString() ?? questionId : questionId,
                QuestionText = questionText,
                Options = new List<string>()
            };

            // Check for custom ratingOptions array
            if (questionElement.TryGetProperty("config", out var configElement))
            {
                if (configElement.TryGetProperty("ratingOptions", out var ratingOptionsElement)
                    && ratingOptionsElement.ValueKind == JsonValueKind.Array)
                {
                    // Custom rating options (e.g., "Unsatisfactory", "Okay", "Satisfactory")
                    foreach (var option in ratingOptionsElement.EnumerateArray())
                    {
                        if (option.TryGetProperty("text", out var textElement))
                        {
                            var text = textElement.GetString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                ratingInfo.Options.Add(text);
                            }
                        }
                    }
                }
            }

            // If no custom options, use numeric rating scale (min, max, step)
            if (ratingInfo.Options.Count == 0)
            {
                var config = questionElement.TryGetProperty("config", out var cfg) ? cfg : questionElement;
                var min = config.TryGetProperty("ratingMin", out var minElement)
                    ? minElement.GetInt32()
                    : config.TryGetProperty("rateMin", out var rateMinElement)
                        ? rateMinElement.GetInt32()
                        : 1;
                var max = config.TryGetProperty("ratingMax", out var maxElement)
                    ? maxElement.GetInt32()
                    : config.TryGetProperty("rateMax", out var rateMaxElement)
                        ? rateMaxElement.GetInt32()
                        : 5;
                var step = config.TryGetProperty("ratingStep", out var stepElement)
                    ? stepElement.GetInt32()
                    : config.TryGetProperty("rateStep", out var rateStepElement)
                        ? rateStepElement.GetInt32()
                        : 1;

                // Generate numeric options
                for (int i = min; i <= max; i += step)
                {
                    ratingInfo.Options.Add(i.ToString());
                }
            }

            if (ratingInfo.Options.Count > 0)
            {
                ratingQuestions[questionId] = ratingInfo;
            }
        }
    }

    /// <summary>
    /// Calculate scores from response data
    /// </summary>
    private ScoreSummaryDto CalculateScores(JsonDocument? responseData, Dictionary<string, RatingQuestionInfo> ratingQuestions)
    {
        var questionScores = new List<QuestionScoreDto>();

        if (responseData == null)
        {
            return new ScoreSummaryDto
            {
                OverallScore = 0,
                TotalQuestions = ratingQuestions.Count,
                AnsweredQuestions = 0,
                QuestionScores = questionScores
            };
        }

        try
        {
            var responseRoot = responseData.RootElement;

            foreach (var kvp in ratingQuestions)
            {
                var questionId = kvp.Key;
                var questionInfo = kvp.Value;

                // Try to get answer from response (could be keyed by name, id, or questionId)
                string? answerValue = null;
                if (responseRoot.TryGetProperty(questionId, out var answerElement))
                {
                    answerValue = answerElement.ValueKind switch
                    {
                        JsonValueKind.String => answerElement.GetString(),
                        JsonValueKind.Number => answerElement.GetInt32().ToString(),
                        _ => answerElement.GetRawText()
                    };
                }

                if (string.IsNullOrEmpty(answerValue))
                    continue;

                // Find position of answer in options
                var position = questionInfo.Options.IndexOf(answerValue);
                if (position < 0)
                    continue; // Answer not found in options

                // Calculate score: position / (totalOptions - 1) * 100
                // Position 0 = 0%, Position (n-1) = 100%
                var totalOptions = questionInfo.Options.Count;
                var score = totalOptions > 1 ? (double)position / (totalOptions - 1) * 100 : 0;

                questionScores.Add(new QuestionScoreDto
                {
                    QuestionId = questionId,
                    QuestionName = questionInfo.QuestionName,
                    QuestionText = questionInfo.QuestionText,
                    Score = Math.Round(score, 2),
                    Position = position,
                    TotalOptions = totalOptions,
                    SelectedOption = answerValue
                });
            }

            var overallScore = questionScores.Any() 
                ? Math.Round(questionScores.Average(q => q.Score), 2)
                : 0;

            return new ScoreSummaryDto
            {
                OverallScore = overallScore,
                TotalQuestions = ratingQuestions.Count,
                AnsweredQuestions = questionScores.Count,
                QuestionScores = questionScores
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating scores from response data");
            return new ScoreSummaryDto
            {
                OverallScore = 0,
                TotalQuestions = ratingQuestions.Count,
                AnsweredQuestions = 0,
                QuestionScores = questionScores
            };
        }
    }

    /// <summary>
    /// Calculate average scores across multiple response data sets
    /// </summary>
    private ScoreSummaryDto CalculateAverageScores(List<JsonDocument?> responseDataList, Dictionary<string, RatingQuestionInfo> ratingQuestions)
    {
        if (!responseDataList.Any())
        {
            return new ScoreSummaryDto
            {
                OverallScore = 0,
                TotalQuestions = ratingQuestions.Count,
                AnsweredQuestions = 0,
                QuestionScores = new List<QuestionScoreDto>()
            };
        }

        // Calculate scores for each response
        var allScores = responseDataList
            .Select(rd => CalculateScores(rd, ratingQuestions))
            .Where(s => s.AnsweredQuestions > 0)
            .ToList();

        if (!allScores.Any())
        {
            return new ScoreSummaryDto
            {
                OverallScore = 0,
                TotalQuestions = ratingQuestions.Count,
                AnsweredQuestions = 0,
                QuestionScores = new List<QuestionScoreDto>()
            };
        }

        // Average question scores across all responses
        var questionScoresDict = new Dictionary<string, List<double>>();

        foreach (var scoreSummary in allScores)
        {
            foreach (var questionScore in scoreSummary.QuestionScores)
            {
                if (!questionScoresDict.ContainsKey(questionScore.QuestionId))
                {
                    questionScoresDict[questionScore.QuestionId] = new List<double>();
                }
                questionScoresDict[questionScore.QuestionId].Add(questionScore.Score);
            }
        }

        // Calculate average scores for each question
        var averageQuestionScores = new List<QuestionScoreDto>();
        foreach (var kvp in ratingQuestions)
        {
            var questionId = kvp.Key;
            var questionInfo = kvp.Value;

            if (questionScoresDict.ContainsKey(questionId) && questionScoresDict[questionId].Any())
            {
                var avgScore = Math.Round(questionScoresDict[questionId].Average(), 2);
                
                // Get representative question details from first response that has this question
                var firstQuestionScore = allScores
                    .SelectMany(s => s.QuestionScores)
                    .FirstOrDefault(q => q.QuestionId == questionId);

                averageQuestionScores.Add(new QuestionScoreDto
                {
                    QuestionId = questionId,
                    QuestionName = questionInfo.QuestionName,
                    QuestionText = questionInfo.QuestionText,
                    Score = avgScore,
                    Position = null, // Average position doesn't make sense
                    TotalOptions = questionInfo.Options.Count,
                    SelectedOption = null // Multiple options averaged
                });
            }
        }

        var overallScore = averageQuestionScores.Any()
            ? Math.Round(averageQuestionScores.Average(q => q.Score), 2)
            : 0;

        return new ScoreSummaryDto
        {
            OverallScore = overallScore,
            TotalQuestions = ratingQuestions.Count,
            AnsweredQuestions = averageQuestionScores.Count,
            QuestionScores = averageQuestionScores
        };
    }

    #endregion

    #region Helper Classes

    private class RatingQuestionInfo
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionName { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
    }

    #endregion
}

