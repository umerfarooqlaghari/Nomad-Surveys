using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class SubjectEvaluatorSurvey
{
    public Guid Id { get; set; }
    
    // Foreign keys
    public Guid SubjectEvaluatorId { get; set; }
    public Guid SurveyId { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Tracks when the last automated reminder was sent
    /// </summary>
    public DateTime? LastReminderSentAt { get; set; }
    
    // Tenant isolation
    public Guid TenantId { get; set; }
    
    // Navigation properties
    public virtual SubjectEvaluator SubjectEvaluator { get; set; } = null!;
    public virtual Survey Survey { get; set; } = null!;
    public virtual Tenant Tenant { get; set; } = null!;
}

