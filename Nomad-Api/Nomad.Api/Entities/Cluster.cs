using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

/// <summary>
/// Cluster entity - top level of the hierarchical structure
/// </summary>
public class Cluster
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string ClusterName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Competency> Competencies { get; set; } = new List<Competency>();
}

