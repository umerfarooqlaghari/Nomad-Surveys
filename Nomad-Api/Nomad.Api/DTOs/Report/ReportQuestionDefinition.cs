namespace Nomad.Api.DTOs.Report;

public class ReportQuestionDefinition
{
    public string QuestionId { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string ClusterName { get; set; } = string.Empty;
    public string CompetencyName { get; set; } = string.Empty;
    public int Order { get; set; }
}
