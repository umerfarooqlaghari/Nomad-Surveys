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
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? LastLoginAt { get; set; }
    
    // Multi-tenant support - nullable for SuperAdmin
    public Guid? TenantId { get; set; }
    
    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<UserTenantRole> UserTenantRoles { get; set; } = new List<UserTenantRole>();
    
    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
