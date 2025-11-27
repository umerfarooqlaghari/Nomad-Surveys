using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Nomad.Api.Entities;

/// <summary>
/// Report template entity for storing customizable report templates
/// </summary>
public class ReportTemplate
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Template JSON schema stored as JSONB in PostgreSQL
    /// Contains page settings, theme, and element definitions
    /// </summary>
    [Required]
    public JsonDocument TemplateSchema { get; set; } = JsonDocument.Parse("{}");

    /// <summary>
    /// Placeholder mappings stored as JSONB
    /// Maps placeholder tokens to API data sources
    /// </summary>
    public JsonDocument? PlaceholderMappings { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}


