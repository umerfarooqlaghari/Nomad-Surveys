using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class SubjectEvaluator
{
    public Guid Id { get; set; }
    
    // Foreign keys
    public Guid SubjectId { get; set; }
    public Guid EvaluatorId { get; set; }
    
    // Relationship type from CSV (DirectReport, Manager, Colleague, Other)
    [MaxLength(50)]
    public string? Relationship { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Tenant isolation (inherited from both Subject and Evaluator)
    public Guid TenantId { get; set; }
    
    // Navigation properties
    public virtual Subject Subject { get; set; } = null!;
    public virtual Evaluator Evaluator { get; set; } = null!;
    public virtual Tenant Tenant { get; set; } = null!;
}
