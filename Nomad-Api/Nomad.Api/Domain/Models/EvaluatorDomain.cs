namespace Nomad.Api.Domain.Models;

public class EvaluatorDomain
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EvaluatorEmail { get; set; } = string.Empty;

    
    // Primary fields
    public string? CompanyName { get; set; }
    public string? Gender { get; set; }
    public string? BusinessUnit { get; set; }
    public string? Grade { get; set; }
    public string? Designation { get; set; }
    public int? Tenure { get; set; }
    public string? Location { get; set; }
    
    // Secondary metadata fields
    public string? Metadata1 { get; set; }
    public string? Metadata2 { get; set; }
    
    // Authentication
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Tenant isolation
    public Guid TenantId { get; set; }
    
    // Related data
    public TenantDomain? Tenant { get; set; }
    public List<SubjectEvaluatorDomain> SubjectEvaluators { get; set; } = new();
    
    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
