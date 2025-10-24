using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(100)]
    public string? Designation { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public int? Tenure { get; set; }

    [MaxLength(50)]
    public string? Grade { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(255)]
    public string? Metadata1 { get; set; }

    [MaxLength(255)]
    public string? Metadata2 { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Multi-tenant support - nullable for SuperAdmin
    public Guid? TenantId { get; set; }

    // FK to Employee (nullable - not all users are employees)
    public Guid? EmployeeId { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual ICollection<UserTenantRole> UserTenantRoles { get; set; } = new List<UserTenantRole>();

    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
