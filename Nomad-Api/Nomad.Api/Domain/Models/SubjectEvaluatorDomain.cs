namespace Nomad.Api.Domain.Models;

public class SubjectEvaluatorDomain
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid EvaluatorId { get; set; }
    public string? Relationship { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Tenant isolation
    public Guid TenantId { get; set; }
    
    // Related data
    public SubjectDomain? Subject { get; set; }
    public EvaluatorDomain? Evaluator { get; set; }
    public TenantDomain? Tenant { get; set; }
}
