using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

/// <summary>
/// Report template settings entity for storing report branding and configuration
/// </summary>
public class ReportTemplateSettings
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Company name displayed on reports
    /// </summary>
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Company logo URL (from Cloudinary or base64)
    /// </summary>
    [MaxLength(1000)]
    public string? CompanyLogoUrl { get; set; }

    /// <summary>
    /// Cover image URL for the first page
    /// </summary>
    [MaxLength(1000)]
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Primary brand color (hex code)
    /// </summary>
    [MaxLength(7)]
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary brand color (hex code)
    /// </summary>
    [MaxLength(7)]
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Tertiary brand color (hex code)
    /// </summary>
    [MaxLength(7)]
    public string? TertiaryColor { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}


