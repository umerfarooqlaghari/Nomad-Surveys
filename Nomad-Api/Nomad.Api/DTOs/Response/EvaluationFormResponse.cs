using System.Text.Json;

namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for evaluation form (survey to fill out)
/// </summary>
public class EvaluationFormResponse
{
    public Guid AssignmentId { get; set; }
    public Guid SurveyId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid EvaluatorId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public string SurveyDescription { get; set; } = string.Empty;
    public JsonDocument SurveySchema { get; set; } = null!;
    public string SubjectName { get; set; } = string.Empty;
    public string EvaluatorName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public JsonDocument? SavedResponseData { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

