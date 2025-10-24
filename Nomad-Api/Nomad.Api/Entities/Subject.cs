using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class Subject
{
    public Guid Id { get; set; }

    // Foreign key to Employee
    [Required]
    public Guid EmployeeId { get; set; }

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
    public virtual Employee Employee { get; set; } = null!;
    public virtual ICollection<SubjectEvaluator> SubjectEvaluators { get; set; } = new List<SubjectEvaluator>();

    // Computed properties from Employee
    public string FullName => Employee?.FullName ?? string.Empty;
    public string FirstName => Employee?.FirstName ?? string.Empty;
    public string LastName => Employee?.LastName ?? string.Empty;
    public string Email => Employee?.Email ?? string.Empty;
}
