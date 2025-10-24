using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

public class ValidationResult
{
    public string EmployeeId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}

public class ValidationResponse
{
    public List<ValidationResult> Results { get; set; } = new();
    public int TotalRequested { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
}

public class EvaluatorRelationship
{
    /// <summary>
    /// The EmployeeId (NOT GUID) of the evaluator (e.g., "EMP0097")
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string EvaluatorEmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Relationship { get; set; } = string.Empty;
}

public class SubjectRelationship
{
    /// <summary>
    /// The EmployeeId (NOT GUID) of the subject (e.g., "SUB001")
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string SubjectEmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Relationship { get; set; } = string.Empty;
}

public class CreateSubjectRequest
{
    /// <summary>
    /// The EmployeeId (NOT GUID) from the Employees table (e.g., "EMP001")
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string EmployeeId { get; set; } = string.Empty;

    // Enhanced relationship support with types (optional - can be added later)
    public List<EvaluatorRelationship>? EvaluatorRelationships { get; set; }
}

public class UpdateSubjectRequest
{
    /// <summary>
    /// The EmployeeId (NOT GUID) from the Employees table (e.g., "EMP001")
    /// Can be updated to link to a different employee
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string EmployeeId { get; set; } = string.Empty;
}

public class BulkCreateSubjectsRequest
{
    [Required]
    [MinLength(1)]
    public List<CreateSubjectRequest> Subjects { get; set; } = new();
}

public class CreateEvaluatorRequest
{
    /// <summary>
    /// The EmployeeId (NOT GUID) from the Employees table (e.g., "EMP001")
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string EmployeeId { get; set; } = string.Empty;

    // Enhanced relationship support with types (optional - can be added later)
    public List<SubjectRelationship>? SubjectRelationships { get; set; }
}

public class UpdateEvaluatorRequest
{
    /// <summary>
    /// The EmployeeId (NOT GUID) from the Employees table (e.g., "EMP001")
    /// Can be updated to link to a different employee
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string EmployeeId { get; set; } = string.Empty;
}

public class BulkCreateEvaluatorsRequest
{
    [Required]
    [MinLength(1)]
    public List<CreateEvaluatorRequest> Evaluators { get; set; } = new();
}

public class AssignEvaluatorsToSubjectRequest
{
    [Required]
    [MinLength(1)]
    public List<EvaluatorAssignmentRequest> Evaluators { get; set; } = new();
}

public class AssignSubjectsToEvaluatorRequest
{
    [Required]
    [MinLength(1)]
    public List<SubjectAssignmentRequest> Subjects { get; set; } = new();
}

public class EvaluatorAssignmentRequest
{
    [Required]
    public Guid EvaluatorId { get; set; }
    
    [StringLength(50)]
    public string? Relationship { get; set; }
}

public class SubjectAssignmentRequest
{
    [Required]
    public Guid SubjectId { get; set; }

    [StringLength(50)]
    public string? Relationship { get; set; }
}

public class UpdateRelationshipRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Relationship { get; set; } = string.Empty;
}
