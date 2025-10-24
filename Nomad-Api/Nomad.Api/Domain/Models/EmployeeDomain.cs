namespace Nomad.Api.Domain.Models;

public class EmployeeDomain
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Number { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public int? Tenure { get; set; }
    public string? Grade { get; set; }
    public string? Gender { get; set; }
    public string? ManagerId { get; set; }
    public string? MoreInfo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? EvaluatorId { get; set; }
    
    // Related data
    public TenantDomain? Tenant { get; set; }
    public SubjectDomain? Subject { get; set; }
    public EvaluatorDomain? Evaluator { get; set; }
    
    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}

