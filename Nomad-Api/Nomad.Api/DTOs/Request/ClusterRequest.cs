using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

/// <summary>
/// Request DTO for creating a new cluster
/// </summary>
public class CreateClusterRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ClusterName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
}

/// <summary>
/// Request DTO for updating an existing cluster
/// </summary>
public class UpdateClusterRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ClusterName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}

