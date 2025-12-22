using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

/// <summary>
/// Entity for storing chart images for reports
/// Supports hierarchical structure: Survey → ImageType → Cluster → Competency
/// </summary>
public class ReportChartImage
{
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to Survey
    /// </summary>
    public Guid SurveyId { get; set; }

    /// <summary>
    /// Type of image: "Page4", "Page6", "Cluster", "Competency"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ImageType { get; set; } = string.Empty;

    /// <summary>
    /// Cluster name (required for Cluster and Competency types)
    /// </summary>
    [MaxLength(255)]
    public string? ClusterName { get; set; }

    /// <summary>
    /// Competency name (required for Competency type only)
    /// </summary>
    [MaxLength(255)]
    public string? CompetencyName { get; set; }

    /// <summary>
    /// URL to the uploaded image (Cloudinary)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string ImageUrl { get; set; } = string.Empty;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Survey Survey { get; set; } = null!;
    public virtual Tenant Tenant { get; set; } = null!;
}

