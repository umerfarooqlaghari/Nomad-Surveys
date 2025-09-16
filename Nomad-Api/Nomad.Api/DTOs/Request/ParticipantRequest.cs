using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

public class CreateSubjectRequest
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
    

    
    // Primary fields
    [StringLength(100)]
    public string? CompanyName { get; set; }
    
    [StringLength(20)]
    public string? Gender { get; set; }
    
    [StringLength(100)]
    public string? BusinessUnit { get; set; }
    
    [StringLength(50)]
    public string? Grade { get; set; }
    
    [StringLength(100)]
    public string? Designation { get; set; }
    
    [Range(0, 100)]
    public int? Tenure { get; set; }
    
    [StringLength(100)]
    public string? Location { get; set; }
    
    // Secondary metadata fields
    [StringLength(500)]
    public string? Metadata1 { get; set; }
    
    [StringLength(500)]
    public string? Metadata2 { get; set; }
}

public class UpdateSubjectRequest
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
    

    
    // Primary fields
    [StringLength(100)]
    public string? CompanyName { get; set; }
    
    [StringLength(20)]
    public string? Gender { get; set; }
    
    [StringLength(100)]
    public string? BusinessUnit { get; set; }
    
    [StringLength(50)]
    public string? Grade { get; set; }
    
    [StringLength(100)]
    public string? Designation { get; set; }
    
    [Range(0, 100)]
    public int? Tenure { get; set; }
    
    [StringLength(100)]
    public string? Location { get; set; }
    
    // Secondary metadata fields
    [StringLength(500)]
    public string? Metadata1 { get; set; }
    
    [StringLength(500)]
    public string? Metadata2 { get; set; }
}

public class BulkCreateSubjectsRequest
{
    [Required]
    [MinLength(1)]
    public List<CreateSubjectRequest> Subjects { get; set; } = new();
}

public class CreateEvaluatorRequest
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
    public string EvaluatorEmail { get; set; } = string.Empty;
    

    
    // Primary fields
    [StringLength(100)]
    public string? CompanyName { get; set; }
    
    [StringLength(20)]
    public string? Gender { get; set; }
    
    [StringLength(100)]
    public string? BusinessUnit { get; set; }
    
    [StringLength(50)]
    public string? Grade { get; set; }
    
    [StringLength(100)]
    public string? Designation { get; set; }
    
    [Range(0, 100)]
    public int? Tenure { get; set; }
    
    [StringLength(100)]
    public string? Location { get; set; }
    
    // Secondary metadata fields
    [StringLength(500)]
    public string? Metadata1 { get; set; }
    
    [StringLength(500)]
    public string? Metadata2 { get; set; }
}

public class UpdateEvaluatorRequest
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
    public string EvaluatorEmail { get; set; } = string.Empty;
    

    
    // Primary fields
    [StringLength(100)]
    public string? CompanyName { get; set; }
    
    [StringLength(20)]
    public string? Gender { get; set; }
    
    [StringLength(100)]
    public string? BusinessUnit { get; set; }
    
    [StringLength(50)]
    public string? Grade { get; set; }
    
    [StringLength(100)]
    public string? Designation { get; set; }
    
    [Range(0, 100)]
    public int? Tenure { get; set; }
    
    [StringLength(100)]
    public string? Location { get; set; }
    
    // Secondary metadata fields
    [StringLength(500)]
    public string? Metadata1 { get; set; }
    
    [StringLength(500)]
    public string? Metadata2 { get; set; }
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
