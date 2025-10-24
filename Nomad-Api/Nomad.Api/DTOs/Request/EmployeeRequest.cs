using System.ComponentModel.DataAnnotations;
using Nomad.Api.DTOs.Common;

namespace Nomad.Api.DTOs.Request;

public class CreateEmployeeRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Number { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string EmployeeId { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CompanyName { get; set; }
    
    [StringLength(100)]
    public string? Designation { get; set; }
    
    [StringLength(100)]
    public string? Department { get; set; }
    
    [Range(0, 100)]
    public int? Tenure { get; set; }
    
    [StringLength(50)]
    public string? Grade { get; set; }
    
    [StringLength(20)]
    public string? Gender { get; set; }
    
    [StringLength(50)]
    public string? ManagerId { get; set; }

    public List<AdditionalAttribute>? MoreInfo { get; set; }

    public Guid? SubjectId { get; set; }
    public Guid? EvaluatorId { get; set; }
}

public class UpdateEmployeeRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Number { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string EmployeeId { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CompanyName { get; set; }
    
    [StringLength(100)]
    public string? Designation { get; set; }
    
    [StringLength(100)]
    public string? Department { get; set; }
    
    [Range(0, 100)]
    public int? Tenure { get; set; }
    
    [StringLength(50)]
    public string? Grade { get; set; }
    
    [StringLength(20)]
    public string? Gender { get; set; }
    
    [StringLength(50)]
    public string? ManagerId { get; set; }

    public List<AdditionalAttribute>? MoreInfo { get; set; }

    public Guid? SubjectId { get; set; }
    public Guid? EvaluatorId { get; set; }
}

public class BulkCreateEmployeesRequest
{
    [Required]
    [MinLength(1)]
    public List<CreateEmployeeRequest> Employees { get; set; } = new();
}

