using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class Evaluator
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string EvaluatorEmail { get; set; } = string.Empty;
    

    
    // Primary fields from CSV
    [MaxLength(100)]
    public string? CompanyName { get; set; }
    
    [MaxLength(20)]
    public string? Gender { get; set; }
    
    [MaxLength(100)]
    public string? BusinessUnit { get; set; }
    
    [MaxLength(50)]
    public string? Grade { get; set; }
    
    [MaxLength(100)]
    public string? Designation { get; set; }
    
    public int? Tenure { get; set; }
    
    [MaxLength(100)]
    public string? Location { get; set; }
    
    // Secondary metadata fields
    [MaxLength(500)]
    public string? Metadata1 { get; set; }
    
    [MaxLength(500)]
    public string? Metadata2 { get; set; }
    
    // Authentication
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Tenant isolation
    public Guid TenantId { get; set; }

    // Link to ApplicationUser for authentication
    public Guid? UserId { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<SubjectEvaluator> SubjectEvaluators { get; set; } = new List<SubjectEvaluator>();
    
    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
