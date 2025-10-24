using System.ComponentModel.DataAnnotations;
using Nomad.Api.DTOs.Common;

namespace Nomad.Api.Entities;

public class Employee
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
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Number { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CompanyName { get; set; }
    
    [MaxLength(100)]
    public string? Designation { get; set; }
    
    [MaxLength(100)]
    public string? Department { get; set; }
    
    public int? Tenure { get; set; }
    
    [MaxLength(50)]
    public string? Grade { get; set; }
    
    [MaxLength(20)]
    public string? Gender { get; set; }
    
    [MaxLength(50)]
    public string? ManagerId { get; set; }

    // List of additional attributes (stored as JSONB in database)
    public List<AdditionalAttribute>? MoreInfo { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Subject? Subject { get; set; }
    public virtual Evaluator? Evaluator { get; set; }
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}

