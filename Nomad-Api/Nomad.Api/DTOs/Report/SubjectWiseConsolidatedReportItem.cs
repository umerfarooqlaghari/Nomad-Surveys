using Nomad.Api.DTOs.Report;

namespace Nomad.Api.DTOs.Report;

public class SubjectWiseConsolidatedReportItem
{
    // Metadata
    public Guid SubmissionId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string BusinessUnit { get; set; } = string.Empty; // Added based on requirements

    // Scores
    // QuestionId -> Score
    public Dictionary<string, double> QuestionScores { get; set; } = new();

    // CompetencyName -> Score (Sum)
    public Dictionary<string, double> CompetencyScores { get; set; } = new();

    // ClusterName -> Score (Sum)
    public Dictionary<string, double> ClusterScores { get; set; } = new();

    // Open Ended
    // QuestionId -> Answer Text
    public Dictionary<string, string> OpenEndedResponses { get; set; } = new();

    // Total Score (Sum of all numeric answers)
    public double TotalScore { get; set; }
}
