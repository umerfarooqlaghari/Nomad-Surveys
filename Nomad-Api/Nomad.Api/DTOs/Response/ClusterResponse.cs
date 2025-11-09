namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for cluster details
/// </summary>
public class ClusterResponse
{
    public Guid Id { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// List of competencies in this cluster
    /// </summary>
    public List<CompetencyResponse>? Competencies { get; set; }
}

/// <summary>
/// Response DTO for cluster list items (lighter version)
/// </summary>
public class ClusterListResponse
{
    public Guid Id { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Count of competencies in this cluster
    /// </summary>
    public int CompetencyCount { get; set; }
}

