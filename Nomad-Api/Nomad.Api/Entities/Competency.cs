using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

/// <summary>
/// Competency entity - middle level of the hierarchical structure
/// </summary>
public class Competency
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    // Foreign key to Cluster
    [Required]
    public Guid ClusterId { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Cluster Cluster { get; set; } = null!;
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}

