using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

/// <summary>
/// Question entity - bottom level of the hierarchical structure
/// Contains separate questions for self-evaluation and others' evaluation
/// </summary>
public class Question
{
    public Guid Id { get; set; }

    // Foreign key to Competency
    [Required]
    public Guid CompetencyId { get; set; }

    /// <summary>
    /// Question text for self-evaluation
    /// Can be longer textual content
    /// </summary>
    [MaxLength(2000)]
    public string? SelfQuestion { get; set; } = string.Empty;

    /// <summary>
    /// Question text for evaluation by others
    /// Can be longer textual content
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string OthersQuestion { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Competency Competency { get; set; } = null!;
}

