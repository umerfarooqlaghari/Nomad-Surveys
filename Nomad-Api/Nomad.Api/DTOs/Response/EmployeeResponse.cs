using Nomad.Api.DTOs.Common;

namespace Nomad.Api.DTOs.Response;

public class EmployeeResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
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
    public List<AdditionalAttribute>? MoreInfo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? EvaluatorId { get; set; }
}

public class EmployeeListResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Number { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
}

