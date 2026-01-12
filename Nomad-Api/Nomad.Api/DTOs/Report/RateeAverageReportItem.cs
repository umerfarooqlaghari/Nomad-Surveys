namespace Nomad.Api.DTOs.Report;

public class RateeAverageReportItem
{
    public Guid SubjectId { get; set; }
    public string? EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Department { get; set; }
    
    // Key: CompetencyName, Value: (SelfScore, OthersScore)
    public Dictionary<string, (double? Self, double? Others)> CompetencyScores { get; set; } = new();
}
