namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for competency details
/// </summary>
public class CompetencyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ClusterId { get; set; }
    public string? ClusterName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// List of questions in this competency
    /// </summary>
    public List<QuestionResponse>? Questions { get; set; }
}

/// <summary>
/// Response DTO for competency list items (lighter version)
/// </summary>
public class CompetencyListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ClusterId { get; set; }
    public string? ClusterName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Count of questions in this competency
    /// </summary>
    public int QuestionCount { get; set; }
}

