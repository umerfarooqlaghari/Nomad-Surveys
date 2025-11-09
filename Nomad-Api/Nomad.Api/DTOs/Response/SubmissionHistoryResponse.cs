using System.Text.Json;

namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for submission history
/// </summary>
public class SubmissionHistoryResponse
{
    public Guid SubmissionId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid SurveyId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectEmail { get; set; } = string.Empty;
    public string SubjectDepartment { get; set; } = string.Empty;
    public string SurveyTitle { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Response DTO for submission details (read-only view)
/// </summary>
public class SubmissionDetailResponse
{
    public Guid SubmissionId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public string SurveyDescription { get; set; } = string.Empty;
    public JsonDocument SurveySchema { get; set; } = null!;
    public JsonDocument ResponseData { get; set; } = null!;
    public string SubjectName { get; set; } = string.Empty;
    public string EvaluatorName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
}

