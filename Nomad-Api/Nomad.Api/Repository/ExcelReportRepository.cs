using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Report;
using Nomad.Api.Entities;
using System.Text.Json;

namespace Nomad.Api.Repository;

public class ExcelReportRepository : IExcelReportRepository
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<ExcelReportRepository> _logger;

    public ExcelReportRepository(
        NomadSurveysDbContext context,
        ILogger<ExcelReportRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    private class QuestionMapping
    {
        public string QuestionName { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string ClusterName { get; set; } = string.Empty;
        public string CompetencyName { get; set; } = string.Empty;
        public int RatingMin { get; set; }
        public int RatingMax { get; set; }
        public Dictionary<string, int> RatingOptionsMap { get; set; } = new();
    }

    /// <summary>
    /// Gets the hierarchy of Clusters and Competencies from the survey schema.
    /// This is used to order columns in the Excel report.
    /// </summary>
    public async Task<ClusterCompetencyHierarchyResult> GetClusterCompetencyHierarchyAsync(Guid subjectId, Guid surveyId, Guid tenantId)
    {
        var result = new ClusterCompetencyHierarchyResult();

        // Get survey
        var survey = await _context.Surveys
            .FirstOrDefaultAsync(s => s.Id == surveyId && s.TenantId == tenantId);

        if (survey?.Schema == null) return result;

        var questionMappings = await ExtractQuestionMappings(survey.Schema, tenantId);

        // Group by Cluster, then Competency
        var grouped = questionMappings.Values
            .Where(q => !string.IsNullOrEmpty(q.ClusterName) && !string.IsNullOrEmpty(q.CompetencyName))
            .GroupBy(q => q.ClusterName)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var clusterGroup in grouped)
        {
            var clusterItem = new ClusterSummaryItem
            {
                ClusterName = clusterGroup.Key
            };

            var competencyGroups = clusterGroup
                .GroupBy(q => q.CompetencyName)
                .OrderBy(g => g.Key);

            foreach (var competencyGroup in competencyGroups)
            {
                clusterItem.Competencies.Add(new CompetencySummaryItem
                {
                    CompetencyName = competencyGroup.Key,
                    ClusterName = clusterGroup.Key
                });
            }

            result.Clusters.Add(clusterItem);
        }

        return result;
    }

    /// <summary>
    /// Gets data for the Ratee Average Report (all subjects, self vs others per competency)
    /// </summary>
    public async Task<List<RateeAverageReportItem>> GetRateeAverageReportDataAsync(Guid surveyId, Guid tenantId)
    {
        var result = new List<RateeAverageReportItem>();

        try
        {
            // Get survey
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && s.TenantId == tenantId);

            if (survey?.Schema == null)
            {
                _logger.LogWarning("Survey not found or has no schema: {SurveyId}", surveyId);
                return result;
            }

            // Extract question mappings
            var questionMappings = await ExtractQuestionMappings(survey.Schema, tenantId);

            // Get clusters and competencies structure for ordering
            var clusters = await GetClusterCompetencyHierarchyAsync(Guid.Empty, surveyId, tenantId);

            // Get ALL subjects assigned to this survey (via active SubjectEvaluatorSurvey)
            var assignedSubjects = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Where(ses => ses.SurveyId == surveyId
                    && ses.IsActive
                    && ses.SubjectEvaluator.IsActive
                    && ses.SubjectEvaluator.Subject.IsActive
                    && ses.SubjectEvaluator.Subject.TenantId == tenantId)
                .Select(ses => ses.SubjectEvaluator.Subject)
                .Distinct()
                .ToListAsync();

            if (!assignedSubjects.Any())
            {
                return result;
            }

            // Get ALL completed submissions for this survey
            var allSubmissions = await _context.SurveySubmissions
                .Include(ss => ss.Subject)
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                        .ThenInclude(se => se.Evaluator)
                .Where(ss => ss.SurveyId == surveyId
                    && ss.TenantId == tenantId
                    && ss.Status == SurveySubmissionStatus.Completed
                    && ss.ResponseData != null
                    && ss.SubjectEvaluatorSurvey != null
                    && ss.SubjectEvaluatorSurvey.IsActive
                    && ss.SubjectEvaluatorSurvey.SubjectEvaluator.IsActive)
                .ToListAsync();

            // Group submissions by SubjectId for easy lookup
            var submissionsBySubject = allSubmissions
                .GroupBy(ss => ss.SubjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var subject in assignedSubjects)
            {
                var item = new RateeAverageReportItem
                {
                    SubjectId = subject.Id,
                    EmployeeId = subject.Employee?.EmployeeId,
                    FullName = subject.FullName,
                    Email = subject.Email,
                    Department = subject.Employee?.Department ?? subject.Employee?.Designation,
                    CompetencyScores = new Dictionary<string, (double? Self, double? Others)>()
                };

                // Get submissions for this subject, empty list if none
                var subjectSubmissions = submissionsBySubject.GetValueOrDefault(subject.Id) ?? new List<SurveySubmission>();

                // Separate Self vs Others
                var selfSubmission = subjectSubmissions.FirstOrDefault(ss =>
                    (ss.SubjectEvaluatorSurvey?.SubjectEvaluator?.Relationship?.ToLower() == "self") ||
                    (ss.SubjectEvaluatorSurvey?.SubjectEvaluator?.Evaluator?.EmployeeId == subject.EmployeeId));

                var otherSubmissions = subjectSubmissions.Where(ss =>
                    !((ss.SubjectEvaluatorSurvey?.SubjectEvaluator?.Relationship?.ToLower() == "self") ||
                      (ss.SubjectEvaluatorSurvey?.SubjectEvaluator?.Evaluator?.EmployeeId == subject.EmployeeId)))
                    .ToList();

                // Calculate Self Scores
                var selfScoresByCompetency = new Dictionary<string, List<double>>();
                if (selfSubmission != null)
                {
                    CalculateCompetencyScores(selfSubmission, questionMappings, selfScoresByCompetency);
                }

                // Calculate Others Scores
                var othersScoresByCompetency = new Dictionary<string, List<double>>();
                foreach (var submission in otherSubmissions)
                {
                    CalculateCompetencyScores(submission, questionMappings, othersScoresByCompetency);
                }

                // Populate Result Item
                // Iterate through known competencies (from schema/clusters) to ensure all columns exist, even if null
                var allCompetencies = questionMappings.Values
                    .Where(q => !string.IsNullOrEmpty(q.CompetencyName))
                    .Select(q => q.CompetencyName)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                foreach (var competency in allCompetencies)
                {
                    double? selfAvg = null;
                    if (selfScoresByCompetency.TryGetValue(competency, out var sScores) && sScores.Any(s => s > 0))
                    {
                        selfAvg = Math.Round(sScores.Where(s => s > 0).Average(), 2); // Exclude 0/NA
                    }

                    double? othersAvg = null;
                    if (othersScoresByCompetency.TryGetValue(competency, out var oScores) && oScores.Any(s => s > 0))
                    {
                        othersAvg = Math.Round(oScores.Where(s => s > 0).Average(), 2); // Exclude 0/NA
                    }

                    item.CompetencyScores[competency] = (selfAvg, othersAvg);
                }

                result.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Ratee Average Report data");
        }

        return result;
    }

    private void CalculateCompetencyScores(
        SurveySubmission submission,
        Dictionary<string, QuestionMapping> questionMappings,
        Dictionary<string, List<double>> competencyScores)
    {
        var questionScores = CalculateScoresForSubmission(submission, questionMappings);

        foreach (var kvp in questionScores)
        {
            if (questionMappings.TryGetValue(kvp.Key, out var mapping) && !string.IsNullOrEmpty(mapping.CompetencyName))
            {
                if (!competencyScores.ContainsKey(mapping.CompetencyName))
                {
                    competencyScores[mapping.CompetencyName] = new List<double>();
                }
                competencyScores[mapping.CompetencyName].Add(kvp.Value);
            }
        }
    }

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

                JsonElement answerElement;
                if (!responseRoot.TryGetProperty(questionId, out answerElement))
                {
                    // Try matrix logic (Composite ID: QuestionId:RowValue)
                    var parts = questionId.Split(':');
                    if (parts.Length == 2 && responseRoot.TryGetProperty(parts[0], out var matrixEl) && matrixEl.ValueKind == JsonValueKind.Object)
                    {
                        if (!matrixEl.TryGetProperty(parts[1], out answerElement))
                            continue;
                    }
                    else
                    {
                        continue;
                    }
                }

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

                if (answerValue.HasValue && answerValue.Value > 0)
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

                    var isRatingType = questionType == "rating" || questionType == "radiogroup" || questionType == "dropdown" || questionType == "matrix";
                    if (!isRatingType)
                        continue;

                    if (string.IsNullOrEmpty(questionId))
                        continue;

                    // Handle Matrix questions
                    if (questionType == "matrix")
                    {
                        if (question.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var row in rows.EnumerateArray())
                            {
                                var rowValue = row.ValueKind == JsonValueKind.Object 
                                    ? (row.TryGetProperty("value", out var rv) ? rv.GetString() : null)
                                    : row.GetString();
                                
                                if (string.IsNullOrEmpty(rowValue)) continue;

                                var compositeId = $"{questionId}:{rowValue}";
                                var rowText = row.ValueKind == JsonValueKind.Object
                                    ? (row.TryGetProperty("text", out var rt) ? rt.GetString() : null)
                                    : row.GetString();
                                
                                var finalQuestionText = rowText ?? rowValue;

                                void ExtractMetadata(JsonElement source, JsonElement fallback, out string? cName, out string? compName)
                                {
                                    cName = null;
                                    compName = null;
                                    var mdSource = source.TryGetProperty("importedFrom", out var Meta) ? Meta : (fallback.TryGetProperty("importedFrom", out var fbMeta) ? fbMeta : default);
                                    
                                    if (mdSource.ValueKind != JsonValueKind.Undefined)
                                    {
                                        if (mdSource.TryGetProperty("clusterId", out var cidEl))
                                        {
                                            var cid = cidEl.GetString();
                                            if (Guid.TryParse(cid, out var gCid) && clusters.TryGetValue(gCid, out var cls)) cName = cls.ClusterName;
                                        }
                                        if (mdSource.TryGetProperty("competencyId", out var compIdEl))
                                        {
                                            var compId = compIdEl.GetString();
                                            if (Guid.TryParse(compId, out var gCompId) && competencies.TryGetValue(gCompId, out var cmp))
                                            {
                                                compName = cmp.Name;
                                                if (cName == null && clusters.TryGetValue(cmp.ClusterId, out var pCls)) cName = pCls.ClusterName;
                                            }
                                        }
                                    }
                                }

                                ExtractMetadata(row, question, out var rowClusterName, out var rowCompetencyName);

                                // Get rating options from question columns
                                var rowRatingOptionsMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                if (question.TryGetProperty("columns", out var cols) && cols.ValueKind == JsonValueKind.Array)
                                {
                                     foreach (var col in cols.EnumerateArray())
                                     {
                                         var val = col.ValueKind == JsonValueKind.Object ? (col.TryGetProperty("value", out var cv) ? cv.ToString() : null) : col.ToString();
                                         var txt = col.ValueKind == JsonValueKind.Object ? (col.TryGetProperty("text", out var ct) ? ct.GetString() : null) : col.GetString();
                                         
                                          int score = 0;
                                          if (int.TryParse(val, out var iVal)) score = iVal;
                                          
                                          if (score > 0) rowRatingOptionsMap[txt ?? val] = score;
                                     }
                                }

                                mappings[compositeId] = new QuestionMapping
                                {
                                    QuestionName = compositeId,
                                    QuestionText = finalQuestionText,
                                    ClusterName = rowClusterName ?? "Uncategorized",
                                    CompetencyName = rowCompetencyName ?? "General",
                                    RatingMin = 1,
                                    RatingMax = 5,
                                    RatingOptionsMap = rowRatingOptionsMap
                                };
                            }
                        }
                        continue;
                    }

                    var questionText = question.TryGetProperty("othersText", out var othersTextEl) ? othersTextEl.GetString() : null;
                    if (string.IsNullOrEmpty(questionText))
                        questionText = question.TryGetProperty("selfText", out var selfTextEl) ? selfTextEl.GetString() : null;
                    if (string.IsNullOrEmpty(questionText))
                        questionText = question.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;

                    string? clusterName = null;
                    string? competencyName = null;

                    if (question.TryGetProperty("importedFrom", out var importedFrom))
                    {
                        if (importedFrom.TryGetProperty("clusterId", out var clusterIdEl))
                        {
                            var clusterIdStr = clusterIdEl.GetString();
                            if (!string.IsNullOrEmpty(clusterIdStr) && Guid.TryParse(clusterIdStr, out var clusterId) && clusters.TryGetValue(clusterId, out var cluster))
                            {
                                clusterName = cluster.ClusterName;
                            }
                        }

                        if (importedFrom.TryGetProperty("competencyId", out var competencyIdEl))
                        {
                            var competencyIdStr = competencyIdEl.GetString();
                            if (!string.IsNullOrEmpty(competencyIdStr) && Guid.TryParse(competencyIdStr, out var competencyId))
                            {
                                if (competencies.TryGetValue(competencyId, out var competency))
                                {
                                    competencyName = competency.Name;
                                    if (string.IsNullOrEmpty(clusterName))
                                    {
                                        if (clusters.TryGetValue(competency.ClusterId, out var parentCluster))
                                        {
                                            clusterName = parentCluster.ClusterName;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var ratingMin = 1;
                    var ratingMax = 5;
                    var ratingOptionsMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    JsonElement config = default;
                    bool hasConfig = question.TryGetProperty("config", out config);

                    if (hasConfig)
                    {
                        if (config.TryGetProperty("ratingMin", out var minEl)) ratingMin = minEl.GetInt32();
                        if (config.TryGetProperty("ratingMax", out var maxEl)) ratingMax = maxEl.GetInt32();
                    }
                    else 
                    {
                         if (question.TryGetProperty("rateMin", out var minEl)) ratingMin = minEl.GetInt32();
                         if (question.TryGetProperty("rateMax", out var maxEl)) ratingMax = maxEl.GetInt32();
                    }

                    void ExtractOptionsScores(JsonElement optionsArray)
                    {
                        foreach (var option in optionsArray.EnumerateArray())
                        {
                            string optionText = null;
                            if (option.ValueKind == JsonValueKind.Object)
                            {
                                optionText = option.TryGetProperty("text", out var textEl) ? textEl.GetString() : 
                                             option.TryGetProperty("value", out var valEl) ? valEl.ToString() : null;
                            }
                            else if (option.ValueKind == JsonValueKind.String)
                            {
                                optionText = option.GetString();
                            }

                            int? score = null;
                            if (option.ValueKind == JsonValueKind.Object)
                            {
                                if (option.TryGetProperty("score", out var scoreEl) && scoreEl.ValueKind == JsonValueKind.Number)
                                {
                                    score = scoreEl.GetInt32();
                                }
                                else if (option.TryGetProperty("Score", out var scoreElCap) && scoreElCap.ValueKind == JsonValueKind.Number)
                                {
                                    score = scoreElCap.GetInt32();
                                }
                            }

                            if (!score.HasValue && option.ValueKind == JsonValueKind.Object) 
                            {
                                 var optionId = option.TryGetProperty("id", out var optIdEl) ? optIdEl.GetString() : null;
                                 if (!string.IsNullOrEmpty(optionId) && int.TryParse(optionId, out var numericId))
                                 {
                                     score = numericId;
                                 }
                                 else if(option.TryGetProperty("order", out var orderEl) && orderEl.ValueKind == JsonValueKind.Number)
                                 {
                                     score = orderEl.GetInt32() + 1;
                                 }
                            }

                            if (!string.IsNullOrEmpty(optionText) && score.HasValue)
                            {
                                ratingOptionsMap[optionText] = score.Value;
                            }
                        }
                    }

                    if (hasConfig && config.TryGetProperty("ratingOptions", out var ratingOptions) && ratingOptions.ValueKind == JsonValueKind.Array)
                    {
                        ExtractOptionsScores(ratingOptions);
                    }
                    
                    if (hasConfig && config.TryGetProperty("options", out var standardOptions) && standardOptions.ValueKind == JsonValueKind.Array)
                    {
                        ExtractOptionsScores(standardOptions);
                    }

                    if (hasConfig && config.TryGetProperty("choices", out var choicesConfig) && choicesConfig.ValueKind == JsonValueKind.Array)
                    {
                        ExtractOptionsScores(choicesConfig);
                    }
                    else if (question.TryGetProperty("choices", out var choicesRoot) && choicesRoot.ValueKind == JsonValueKind.Array)
                    {
                         ExtractOptionsScores(choicesRoot);
                    }

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
}
