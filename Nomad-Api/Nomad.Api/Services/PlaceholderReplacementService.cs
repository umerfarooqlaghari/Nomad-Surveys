using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nomad.Api.Services;

/// <summary>
/// Service for replacing placeholders in templates with actual data
/// </summary>
public class PlaceholderReplacementService : IPlaceholderReplacementService
{
    private readonly ILogger<PlaceholderReplacementService> _logger;

    public PlaceholderReplacementService(ILogger<PlaceholderReplacementService> logger)
    {
        _logger = logger;
    }

    public async Task<JsonDocument> ReplacePlaceholdersAsync(
        JsonDocument templateSchema,
        ComprehensiveReportResponse reportData,
        Dictionary<string, object> additionalData)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Convert template to string for replacement
                var templateJson = templateSchema.RootElement.GetRawText();

                // Create data dictionary for placeholder replacement
                var dataContext = BuildDataContext(reportData, additionalData);

                // Replace placeholders in the template
                var processedJson = ReplacePlaceholders(templateJson, dataContext);

                // Parse back to JsonDocument
                return JsonDocument.Parse(processedJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing placeholders in template");
                throw;
            }
        });
    }

    private Dictionary<string, object> BuildDataContext(
        ComprehensiveReportResponse reportData,
        Dictionary<string, object> additionalData)
    {
        var context = new Dictionary<string, object>
        {
            // Subject information
            ["subject_id"] = reportData.SubjectId.ToString(),
            ["subject_name"] = reportData.SubjectName,
            ["survey_id"] = reportData.SurveyId.ToString(),
            ["survey_title"] = reportData.SurveyTitle,

            // Self-evaluation scores
            ["self_evaluation_score"] = reportData.SelfEvaluation?.OverallScore ?? 0,
            ["self_evaluation_total_questions"] = reportData.SelfEvaluation?.TotalQuestions ?? 0,
            ["self_evaluation_answered_questions"] = reportData.SelfEvaluation?.AnsweredQuestions ?? 0,

            // Evaluator scores
            ["evaluator_average_score"] = reportData.EvaluatorAverage?.OverallScore ?? 0,
            ["evaluator_count"] = reportData.EvaluatorCount,
            ["evaluator_total_questions"] = reportData.EvaluatorAverage?.TotalQuestions ?? 0,
            ["evaluator_answered_questions"] = reportData.EvaluatorAverage?.AnsweredQuestions ?? 0,

            // Self vs Evaluator comparison
            ["self_vs_evaluator_difference"] = reportData.SelfVsEvaluatorDifference,
            ["self_vs_evaluator_percentage_difference"] = reportData.SelfVsEvaluatorPercentageDifference,

            // Organization comparison
            ["organization_average_score"] = reportData.OrganizationAverageScore,
            ["subject_vs_organization_difference"] = reportData.SubjectVsOrganizationDifference,
            ["subject_vs_organization_percentage_difference"] = reportData.SubjectVsOrganizationPercentageDifference,
            ["performance_level"] = reportData.PerformanceLevel.ToString(),
            ["total_subjects_in_org"] = reportData.TotalSubjectsInOrg,

            // Date/time
            ["generated_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generated_time"] = DateTime.UtcNow.ToString("HH:mm:ss"),
            ["generated_datetime"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Add additional data
        foreach (var kvp in additionalData)
        {
            context[kvp.Key] = kvp.Value;
        }

        // Add question-level data
        context["self_evaluation_questions"] = reportData.SelfEvaluation?.QuestionScores ?? new List<QuestionScoreDto>();
        context["evaluator_average_questions"] = reportData.EvaluatorAverage?.QuestionScores ?? new List<QuestionScoreDto>();
        context["self_vs_evaluator_questions"] = reportData.SelfVsEvaluatorQuestions;
        context["subject_vs_organization_questions"] = reportData.SubjectVsOrganizationQuestions;

        return context;
    }

    private string ReplacePlaceholders(string template, Dictionary<string, object> dataContext)
    {
        // Pattern to match placeholders like {{key}} or {{key.property}}
        var pattern = @"\{\{([^}]+)\}\}";
        
        return Regex.Replace(template, pattern, match =>
        {
            var placeholder = match.Groups[1].Value.Trim();
            
            try
            {
                // Handle nested properties like "subject.name"
                var value = ResolvePlaceholderValue(placeholder, dataContext);
                
                if (value == null)
                {
                    _logger.LogWarning("Placeholder not found: {Placeholder}", placeholder);
                    return match.Value; // Return original placeholder if not found
                }

                // Convert value to string representation
                return FormatValue(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving placeholder: {Placeholder}", placeholder);
                return match.Value; // Return original placeholder on error
            }
        });
    }

    private object? ResolvePlaceholderValue(string placeholder, Dictionary<string, object> dataContext)
    {
        // Split by dot to handle nested properties
        var parts = placeholder.Split('.');
        
        if (parts.Length == 0)
        {
            return null;
        }

        // Get first level value
        if (!dataContext.TryGetValue(parts[0], out var value))
        {
            // Try with underscore instead of dot
            var underscoredKey = placeholder.Replace(".", "_");
            if (dataContext.TryGetValue(underscoredKey, out value))
            {
                return value;
            }
            
            return null;
        }

        // If no nested properties, return value as-is
        if (parts.Length == 1)
        {
            return value;
        }

        // Handle nested properties using reflection or dictionary access
        // For now, return the top-level value
        // TODO: Implement full nested property resolution
        return value;
    }

    private string FormatValue(object value)
    {
        if (value == null)
        {
            return "";
        }

        return value switch
        {
            string str => str,
            int or long or short or byte => value.ToString()!,
            float or double or decimal => ((double)value).ToString("F2"),
            bool b => b.ToString().ToLower(),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            _ => value.ToString() ?? ""
        };
    }
}


