using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Nomad.Api.Entities;

/// <summary>
/// Tenant-level settings entity for storing default question configurations
/// These settings apply to all surveys within a tenant
/// </summary>
public class TenantSettings
{
    public Guid Id { get; set; }

    /// <summary>
    /// Default question type for new questions
    /// Possible values: 'rating', 'single-choice', 'multiple-choice', 'text', 'textarea', 'dropdown', 'yes-no', 'date', 'number'
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DefaultQuestionType { get; set; } = "rating";

    /// <summary>
    /// Default rating options stored as JSONB in PostgreSQL
    /// Structure: [{ "id": "opt1", "text": "Very Unsatisfied", "order": 0 }, ...]
    /// </summary>
    public JsonDocument? DefaultRatingOptions { get; set; }

    /// <summary>
    /// Number of default rating options (for validation)
    /// </summary>
    public int? NumberOfOptions { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation (one-to-one with Tenant)
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}

