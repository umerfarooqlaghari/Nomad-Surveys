namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Question score information
/// </summary>
public class QuestionScoreDto
{
    public string QuestionId { get; set; } = string.Empty;
    public string QuestionName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public double Score { get; set; } // 0-100
    public int? Position { get; set; } // Position in rating options (0-based)
    public int TotalOptions { get; set; }
    public string? SelectedOption { get; set; } // The actual option value selected
}

/// <summary>
/// Summary of scores for a subject
/// </summary>
public class ScoreSummaryDto
{
    public double OverallScore { get; set; } // 0-100, average of all question scores
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public List<QuestionScoreDto> QuestionScores { get; set; } = new();
}

/// <summary>
/// Response for subject report
/// </summary>
public class SubjectReportResponse
{
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public ScoreSummaryDto? SelfEvaluation { get; set; }
    public ScoreSummaryDto? EvaluatorAverage { get; set; }
}

/// <summary>
/// Comparison between self-evaluation and evaluator average
/// </summary>
public class SelfVsEvaluatorComparisonResponse
{
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public double SelfEvaluationScore { get; set; } // 0-100
    public double EvaluatorAverageScore { get; set; } // 0-100
    public double ScoreDifference { get; set; } // Positive = self higher, Negative = evaluators higher
    public double PercentageDifference { get; set; } // Percentage difference relative to evaluator average
    public List<QuestionComparisonDto> QuestionComparisons { get; set; } = new();
    public int EvaluatorCount { get; set; } // Number of evaluators who provided feedback
}

/// <summary>
/// Question-level comparison
/// </summary>
public class QuestionComparisonDto
{
    public string QuestionId { get; set; } = string.Empty;
    public string QuestionName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public double SelfScore { get; set; }
    public double EvaluatorAverageScore { get; set; }
    public double ScoreDifference { get; set; }
}

/// <summary>
/// Organization-wide comparison
/// </summary>
public class OrganizationComparisonResponse
{
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public double SubjectOverallScore { get; set; } // 0-100
    public double OrganizationAverageScore { get; set; } // 0-100
    public double ScoreDifference { get; set; } // Positive = above par, Negative = below par
    public double PercentageDifference { get; set; } // Percentage difference relative to org average
    public PerformanceLevel PerformanceLevel { get; set; } // AbovePar, BelowPar, AtPar
    public int TotalSubjectsInOrg { get; set; } // Total subjects with completed submissions
    public List<QuestionComparisonDto> QuestionComparisons { get; set; } = new();
}

/// <summary>
/// Performance level indicator
/// </summary>
public enum PerformanceLevel
{
    AbovePar,
    AtPar,
    BelowPar
}

/// <summary>
/// Comprehensive report combining all metrics
/// </summary>
public class ComprehensiveReportResponse
{
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    
    // Self-evaluation summary
    public ScoreSummaryDto? SelfEvaluation { get; set; }
    
    // Evaluator summary
    public ScoreSummaryDto? EvaluatorAverage { get; set; }
    public int EvaluatorCount { get; set; }
    
    // Self vs Evaluator comparison
    public double SelfVsEvaluatorDifference { get; set; }
    public double SelfVsEvaluatorPercentageDifference { get; set; }
    
    // Organization comparison
    public double OrganizationAverageScore { get; set; }
    public double SubjectVsOrganizationDifference { get; set; }
    public double SubjectVsOrganizationPercentageDifference { get; set; }
    public PerformanceLevel PerformanceLevel { get; set; }
    public int TotalSubjectsInOrg { get; set; }
    
    // Question-level details
    public List<QuestionComparisonDto> SelfVsEvaluatorQuestions { get; set; } = new();
    public List<QuestionComparisonDto> SubjectVsOrganizationQuestions { get; set; } = new();
}


