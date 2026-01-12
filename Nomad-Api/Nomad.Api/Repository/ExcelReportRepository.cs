using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Report;
using Nomad.Api.Entities;
using System.Text.Json;
using System.Globalization;

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
        public string QuestionType { get; set; } = "rating";
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

    public async Task<List<SubjectWiseHeatMapItem>> GetSubjectWiseHeatMapDataAsync(Guid surveyId, Guid tenantId)
    {
        var result = new List<SubjectWiseHeatMapItem>();

        try
        {
            // 1. Get all relevant assignments (active)
            var assignments = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Where(ses => ses.SurveyId == surveyId
                    && ses.SubjectEvaluator.Subject.TenantId == tenantId
                    && ses.IsActive
                    && ses.SubjectEvaluator.IsActive
                    && ses.SubjectEvaluator.Subject.IsActive)
                .ToListAsync();

            if (!assignments.Any()) return result;

            // 2. Get completed assignments IDs
            var completedAssignmentIds = await _context.SurveySubmissions
                .Where(ss => ss.SurveyId == surveyId
                    && ss.TenantId == tenantId
                    && ss.Status == SurveySubmissionStatus.Completed
                    && ss.ResponseData != null
                    && ss.SubjectEvaluatorSurvey != null
                    && ss.SubjectEvaluatorSurvey.IsActive)
                .Select(ss => ss.SubjectEvaluatorSurveyId)
                .ToListAsync();

            var completedSet = new HashSet<Guid>(completedAssignmentIds);

            // 3. Group by Subject
            var assignmentsBySubject = assignments
                .GroupBy(a => a.SubjectEvaluator.SubjectId)
                .ToList();

            foreach (var group in assignmentsBySubject)
            {
                var subject = group.First().SubjectEvaluator.Subject;
                var item = new SubjectWiseHeatMapItem
                {
                    SubjectId = subject.Id,
                    EmployeeId = subject.Employee?.EmployeeId,
                    FullName = subject.FullName,
                    Email = subject.Email,
                    Department = subject.Employee?.Department ?? subject.Employee?.Designation
                };

                // Group by Relationship
                var relationships = group.GroupBy(a => a.SubjectEvaluator.Relationship);

                foreach (var relGroup in relationships)
                {
                    var rawRel = relGroup.Key;
                    string relName = "Unknown";
                    if (!string.IsNullOrWhiteSpace(rawRel))
                    {
                        // Normalize to Title Case
                        relName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawRel.ToLower());
                    }

                    var stats = new RelationshipStats();
                    stats.Sent = relGroup.Count();
                    stats.Completed = relGroup.Count(a => completedSet.Contains(a.Id));

                    item.RelationshipData[relName] = stats;

                    // Add to Grand Total
                    item.GrandTotal.Sent += stats.Sent;
                    item.GrandTotal.Completed += stats.Completed;
                }

                result.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Subject Wise Heat Map data");
        }

        return result;
    }

    public async Task<List<SubjectWiseConsolidatedReportItem>> GetSubjectWiseConsolidatedDataAsync(Guid surveyId, Guid tenantId)
    {
        var result = new List<SubjectWiseConsolidatedReportItem>();

        try
        {
            // Get survey to extract schema
            var survey = await _context.Surveys.FirstOrDefaultAsync(s => s.Id == surveyId && s.TenantId == tenantId);
            if (survey?.Schema == null) return result;

            var questionMappings = await ExtractQuestionMappings(survey.Schema, tenantId);

            // Get all completed submissions for this survey
            var submissions = await _context.SurveySubmissions
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                .Where(ss => ss.SurveyId == surveyId
                    && ss.TenantId == tenantId
                    && ss.Status == SurveySubmissionStatus.Completed
                    && ss.ResponseData != null
                    && ss.SubjectEvaluatorSurvey != null)
                .ToListAsync();

            foreach (var sub in submissions)
            {
                var subject = sub.Subject;
                var evaluator = sub.SubjectEvaluatorSurvey?.SubjectEvaluator;
                if (subject == null || evaluator == null) continue;

                var item = new SubjectWiseConsolidatedReportItem
                {
                    SubmissionId = sub.Id,
                    EmployeeId = subject.Employee?.EmployeeId ?? string.Empty,
                    FullName = subject.FullName,
                    Email = subject.Email,
                    Department = subject.Employee?.Department ?? string.Empty,
                    Designation = subject.Employee?.Designation ?? string.Empty,
                    BusinessUnit = !string.IsNullOrEmpty(subject.Employee?.Department) ? subject.Employee.Department : 
                                   (!string.IsNullOrEmpty(subject.Employee?.Designation) ? subject.Employee.Designation : string.Empty),
                    Relationship = evaluator.Relationship
                };

                // Parse Answers
                if (sub.ResponseData != null)
                {
                    var root = sub.ResponseData.RootElement;
                    
                    foreach (var mapping in questionMappings)
                    {
                        var qId = mapping.Key;
                        var qInfo = mapping.Value;

                        // Extract Answer
                        JsonElement answerElement;
                        bool found = false;

                        if (root.TryGetProperty(qId, out answerElement))
                        {
                            found = true;
                        }
                        else
                        {
                             // Try matrix
                             var parts = qId.Split(':');
                             if (parts.Length == 2 && root.TryGetProperty(parts[0], out var matrixEl) && matrixEl.ValueKind == JsonValueKind.Object)
                             {
                                 if (matrixEl.TryGetProperty(parts[1], out answerElement)) found = true;
                             }
                        }

                        if (!found) continue;

                        // Handle Open Ended
                        if (qInfo.QuestionType == "text" || qInfo.QuestionType == "comment" || qInfo.QuestionType == "textarea")
                        {
                            var text = answerElement.ValueKind == JsonValueKind.String ? answerElement.GetString() : answerElement.ToString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                item.OpenEndedResponses[qId] = text;
                            }
                        }
                        // Handle Rated
                        else
                        {
                            double score = 0;
                            if (answerElement.ValueKind == JsonValueKind.Number)
                            {
                                score = answerElement.GetDouble();
                            }
                            else if (answerElement.ValueKind == JsonValueKind.String)
                            {
                                var text = answerElement.GetString();
                                if (double.TryParse(text, out var dVal)) score = dVal;
                                else if (!string.IsNullOrEmpty(text) && qInfo.RatingOptionsMap.TryGetValue(text, out var mapped)) score = mapped;
                            }

                            if (score > 0)
                            {
                                item.QuestionScores[qId] = score;
                                item.TotalScore += score;

                                // Aggregate to Competency
                                if (!string.IsNullOrEmpty(qInfo.CompetencyName))
                                {
                                    if (!item.CompetencyScores.ContainsKey(qInfo.CompetencyName)) item.CompetencyScores[qInfo.CompetencyName] = 0;
                                    item.CompetencyScores[qInfo.CompetencyName] += score;
                                }
                            }
                        }
                    }

                    // Aggregate Competencies to Clusters
                    // Iterate question mappings to find cluster-competency relations or use pre-calculated map
                    // Better: iterate the computed competency scores and find their cluster
                    var compToCluster = questionMappings.Values
                        .Where(q => !string.IsNullOrEmpty(q.CompetencyName) && !string.IsNullOrEmpty(q.ClusterName))
                        .DistinctBy(q => q.CompetencyName)
                        .ToDictionary(q => q.CompetencyName, q => q.ClusterName);

                    foreach (var compScore in item.CompetencyScores)
                    {
                        if (compToCluster.TryGetValue(compScore.Key, out var clusterName))
                        {
                             if (!item.ClusterScores.ContainsKey(clusterName)) item.ClusterScores[clusterName] = 0;
                             item.ClusterScores[clusterName] += compScore.Value;
                        }
                    }
                }

                result.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Subject Wise Consolidated data");
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

    public async Task<List<ReportQuestionDefinition>> GetQuestionColumnsAsync(Guid surveyId, Guid tenantId)
    {
        var result = new List<ReportQuestionDefinition>();
        try
        {
            var survey = await _context.Surveys.FirstOrDefaultAsync(s => s.Id == surveyId && s.TenantId == tenantId);
            if (survey?.Schema == null) return result;

            var mappings = await ExtractQuestionMappings(survey.Schema, tenantId);

            // Convert to DTO list, ordering is implicit in Dictionary insertion order usually, 
            // but effectively we want them grouped by Cluster -> Competency
            
            // Group by Cluster, Competency
            var grouped = mappings.Values
                .OrderBy(q => q.ClusterName)
                .ThenBy(q => q.CompetencyName)
                //.ThenBy(q => q.QuestionName) // Basic ordering
                .ToList();

            foreach (var q in grouped)
            {
                result.Add(new ReportQuestionDefinition
                {
                    QuestionId = q.QuestionName,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    ClusterName = q.ClusterName,
                    CompetencyName = q.CompetencyName
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question columns");
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
                JsonElement elements;
                if (!page.TryGetProperty("questions", out elements))
                    if (!page.TryGetProperty("elements", out elements))
                        continue;

                if (elements.ValueKind != JsonValueKind.Array)
                    continue;

                ExtractQuestionsRecursively(elements, mappings, clusters, competencies);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting question mappings from schema");
        }

        return mappings;
    }

    private void ExtractQuestionsRecursively(
        JsonElement elements, 
        Dictionary<string, QuestionMapping> mappings,
        Dictionary<Guid, Cluster> clusters, 
        Dictionary<Guid, Competency> competencies)
    {
        foreach (var element in elements.EnumerateArray())
        {
            var type = element.TryGetProperty("type", out var typeEl) ? typeEl.GetString()?.ToLowerInvariant() : null;
            
            // Handle Panel (Recursive)
            if (type == "panel")
            {
                if (element.TryGetProperty("elements", out var innerElements) && innerElements.ValueKind == JsonValueKind.Array)
                {
                    ExtractQuestionsRecursively(innerElements, mappings, clusters, competencies);
                }
                continue;
            }

            var questionId = element.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(questionId)) continue;
            
            var questionName = element.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            
            // Handle Multiple Text
            if (type == "multipletext")
            {
                if (element.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var itemName = item.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                        if (string.IsNullOrEmpty(itemName)) continue;

                        var itemTitle = item.TryGetProperty("title", out var tEl) ? tEl.GetString() : itemName;
                        
                        // Composite ID matching implementation: questionId:itemName (similar to matrix)
                        // However, SurveyJS might store it as an object { "q1": { "item1": "val" } }
                        // The ExcelReportService parsing logic for matrix handles "part1:part2" by looking for object property.
                        // We should reuse that convention.
                        var compositeId = $"{questionId}:{itemName}";

                        mappings[compositeId] = new QuestionMapping
                        {
                            QuestionName = compositeId,
                            QuestionText = itemTitle,
                            ClusterName = "Uncategorized",
                            CompetencyName = "General", // Usually text questions don't have competency mappings
                            QuestionType = "text" // Treat as text for reporting
                        };
                    }
                }
                continue;
            }

            var isRatingType = type == "rating" || type == "radiogroup" || type == "dropdown" || type == "matrix";
            var isTextType = type == "text" || type == "comment" || type == "textarea";

            if (!isRatingType && !isTextType)
                continue;

            // Handle Matrix questions
            if (type == "matrix")
            {
                if (element.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
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

                        ExtractClusterAndCompetencyMetadata(row, element, clusters, competencies, out var rowClusterName, out var rowCompetencyName);

                        // Get rating options from question columns
                        var rowRatingOptionsMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        if (element.TryGetProperty("columns", out var cols) && cols.ValueKind == JsonValueKind.Array)
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
                            RatingOptionsMap = rowRatingOptionsMap,
                            QuestionType = "matrix"
                        };
                    }
                }
                continue;
            }

            var questionText = element.TryGetProperty("othersText", out var othersTextEl) ? othersTextEl.GetString() : null;
            if (string.IsNullOrEmpty(questionText))
                questionText = element.TryGetProperty("selfText", out var selfTextEl) ? selfTextEl.GetString() : null;
            if (string.IsNullOrEmpty(questionText))
                questionText = element.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;

            string? clusterName = null;
            string? competencyName = null;

            if (element.TryGetProperty("importedFrom", out var importedFrom))
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
            bool hasConfig = element.TryGetProperty("config", out config);

            if (hasConfig)
            {
                if (config.TryGetProperty("ratingMin", out var minEl)) ratingMin = minEl.GetInt32();
                if (config.TryGetProperty("ratingMax", out var maxEl)) ratingMax = maxEl.GetInt32();
            }
            else 
            {
                    if (element.TryGetProperty("rateMin", out var minEl)) ratingMin = minEl.GetInt32();
                    if (element.TryGetProperty("rateMax", out var maxEl)) ratingMax = maxEl.GetInt32();
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
            else if (element.TryGetProperty("choices", out var choicesRoot) && choicesRoot.ValueKind == JsonValueKind.Array)
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
                RatingOptionsMap = ratingOptionsMap,
                QuestionType = (type == "text" || type == "comment" || type == "textarea") ? type : "rating"
            };
        }
    }

    public async Task<string> GetSurveyAndTenantDetailsAsync(Guid surveyId, Guid tenantId)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        string companyName = tenant?.Name ?? "Company";

        return companyName;
    }

    private void ExtractClusterAndCompetencyMetadata(
        JsonElement source, 
        JsonElement fallback, 
        Dictionary<Guid, Cluster> clusters,
        Dictionary<Guid, Competency> competencies,
        out string? clusterName, 
        out string? competencyName)
    {
        clusterName = null;
        competencyName = null;
        
        var mdSource = source.TryGetProperty("importedFrom", out var metaProp) ? metaProp : (fallback.TryGetProperty("importedFrom", out var fallbackMeta) ? fallbackMeta : default);
        
        if (mdSource.ValueKind != JsonValueKind.Undefined)
        {
            if (mdSource.TryGetProperty("clusterId", out var cidEl))
            {
                var cid = cidEl.GetString();
                if (Guid.TryParse(cid, out var gCid) && clusters.TryGetValue(gCid, out var cls)) clusterName = cls.ClusterName;
            }
            if (mdSource.TryGetProperty("competencyId", out var compIdEl))
            {
                var compId = compIdEl.GetString();
                if (Guid.TryParse(compId, out var gCompId) && competencies.TryGetValue(gCompId, out var cmp))
                {
                    competencyName = cmp.Name;
                    if (clusterName == null && clusters.TryGetValue(cmp.ClusterId, out var pCls)) clusterName = pCls.ClusterName;
                }
            }
        }
    }
}
