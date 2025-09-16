namespace Nomad.Api.DTOs.Response;

public class SubjectResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    
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
    
    public bool IsActive { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Tenant information
    public Guid TenantId { get; set; }
    public TenantResponse? Tenant { get; set; }
    
    // Related evaluators
    public List<SubjectEvaluatorResponse> Evaluators { get; set; } = new();
}

public class SubjectListResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Designation { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Guid TenantId { get; set; }
    public int EvaluatorCount { get; set; }
}

public class EvaluatorResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
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
    
    public bool IsActive { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Tenant information
    public Guid TenantId { get; set; }
    public TenantResponse? Tenant { get; set; }
    
    // Related subjects
    public List<SubjectEvaluatorResponse> Subjects { get; set; } = new();
}

public class EvaluatorListResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EvaluatorEmail { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Designation { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Guid TenantId { get; set; }
    public int SubjectCount { get; set; }
}

public class SubjectEvaluatorResponse
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid EvaluatorId { get; set; }
    public string? Relationship { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    
    // Related entities (minimal info to avoid circular references)
    public SubjectSummaryResponse? Subject { get; set; }
    public EvaluatorSummaryResponse? Evaluator { get; set; }
}

public class SubjectSummaryResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string? Designation { get; set; }
    public bool IsActive { get; set; }
}

public class EvaluatorSummaryResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EvaluatorEmail { get; set; } = string.Empty;

    public string? Designation { get; set; }
    public bool IsActive { get; set; }
}

public class BulkCreateResponse
{
    public int TotalRequested { get; set; }
    public int SuccessfullyCreated { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Guid> CreatedIds { get; set; } = new();
}

public class AssignmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<SubjectEvaluatorResponse> Assignments { get; set; } = new();
}
