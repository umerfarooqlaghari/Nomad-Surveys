namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for question details
/// </summary>
public class QuestionResponse
{
    public Guid Id { get; set; }
    public Guid CompetencyId { get; set; }
    public string? CompetencyName { get; set; }
    public string SelfQuestion { get; set; } = string.Empty;
    public string OthersQuestion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>
/// Response DTO for question list items (lighter version)
/// </summary>
public class QuestionListResponse
{
    public Guid Id { get; set; }
    public Guid CompetencyId { get; set; }
    public string? CompetencyName { get; set; }
    public string SelfQuestion { get; set; } = string.Empty;
    public string OthersQuestion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

