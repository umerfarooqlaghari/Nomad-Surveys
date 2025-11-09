using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Nomad.Api.Entities;

/// <summary>
/// Survey entity for storing dynamic survey schemas created with SurveyJS
/// </summary>
public class Survey
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// SurveyJS JSON schema stored as JSONB in PostgreSQL
    /// Supports dynamic placeholders like {subjectName}, {evaluatorName}
    /// Questions can have conditional visibility based on relationship type
    /// </summary>
    [Required]
    public JsonDocument Schema { get; set; } = JsonDocument.Parse("{}");

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Tenant isolation
    public Guid TenantId { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}

