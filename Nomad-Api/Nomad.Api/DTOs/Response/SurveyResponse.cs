namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for survey details
/// </summary>
public class SurveyResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public object Schema { get; set; } = new { };
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>
/// Response DTO for survey list items (lighter version)
/// </summary>
public class SurveyListResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Count of questions in the survey (calculated from schema)
    /// </summary>
    public int QuestionCount { get; set; }
}

