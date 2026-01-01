namespace Nomad.Api.DTOs.Response;

public class SubjectResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }

    // Employee data (from linked Employee record)
    public EmployeeResponse? Employee { get; set; }

    // Convenience properties from Employee
    public string FirstName => Employee?.FirstName ?? string.Empty;
    public string LastName => Employee?.LastName ?? string.Empty;
    public string FullName => Employee?.FullName ?? string.Empty;
    public string Email => Employee?.Email ?? string.Empty;
    public string EmployeeIdString => Employee?.EmployeeId ?? string.Empty;
    public string? CompanyName => Employee?.CompanyName;
    public string? Gender => Employee?.Gender;
    public string? Grade => Employee?.Grade;
    public string? Designation => Employee?.Designation;
    public int? Tenure => Employee?.Tenure;
    public string? Department => Employee?.Department;

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

    // Assigned evaluator IDs for easy access
    public List<Guid> AssignedEvaluatorIds { get; set; } = new();
}

public class SubjectListResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }

    // Employee data (from linked Employee record)
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmployeeIdString { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Guid TenantId { get; set; }
    public int EvaluatorCount { get; set; }
    public string EvaluationsReceived { get; set; } = string.Empty;
    public string EvaluationsCompleted { get; set; } = string.Empty;
    public bool IsEvaluator { get; set; }
}

public class EvaluatorResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }

    // Employee data (from linked Employee record)
    public EmployeeResponse? Employee { get; set; }

    // Convenience properties from Employee
    public string FirstName => Employee?.FirstName ?? string.Empty;
    public string LastName => Employee?.LastName ?? string.Empty;
    public string FullName => Employee?.FullName ?? string.Empty;
    public string Email => Employee?.Email ?? string.Empty;
    public string EvaluatorEmail => Employee?.Email ?? string.Empty;
    public string EmployeeIdString => Employee?.EmployeeId ?? string.Empty;
    public string? CompanyName => Employee?.CompanyName;
    public string? Gender => Employee?.Gender;
    public string? Grade => Employee?.Grade;
    public string? Designation => Employee?.Designation;
    public int? Tenure => Employee?.Tenure;
    public string? Department => Employee?.Department;

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

    // Assigned subject IDs for easy access
    public List<Guid> AssignedSubjectIds { get; set; } = new();
}

public class EvaluatorListResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }

    // Employee data (from linked Employee record)
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EvaluatorEmail { get; set; } = string.Empty;
    public string EmployeeIdString { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Guid TenantId { get; set; }
    public int SubjectCount { get; set; }
    public string EvaluationsCompleted { get; set; } = string.Empty;
    public string EvaluationsReceived { get; set; } = string.Empty;
    public bool IsSubject { get; set; }
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
    public Guid EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmployeeIdString { get; set; } = string.Empty;

    public string? Designation { get; set; }
    public bool IsActive { get; set; }
}

public class EvaluatorSummaryResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EvaluatorEmail { get; set; } = string.Empty;
    public string EmployeeIdString { get; set; } = string.Empty;

    public string? Designation { get; set; }
    public bool IsActive { get; set; }
}

public class BulkCreateResponse
{
    public int TotalRequested { get; set; }
    public int SuccessfullyCreated { get; set; }
    public int UpdatedCount { get; set; }
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
