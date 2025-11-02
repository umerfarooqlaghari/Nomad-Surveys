namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for assigned evaluations list
/// </summary>
public class AssignedEvaluationResponse
{
    public Guid AssignmentId { get; set; }
    public Guid SurveyId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid EvaluatorId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectEmail { get; set; } = string.Empty;
    public string SubjectDepartment { get; set; } = string.Empty;
    public string SubjectDesignation { get; set; } = string.Empty;
    public string SurveyTitle { get; set; } = string.Empty;
    public string SurveyDescription { get; set; } = string.Empty;
    public bool IsSelfEvaluation { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, InProgress, Completed
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? DueDate { get; set; }
}

