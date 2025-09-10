using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class TenantRole : IdentityRole<Guid>
{
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Multi-tenant support - nullable for global roles like SuperAdmin
    public Guid? TenantId { get; set; }
    
    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<UserTenantRole> UserTenantRoles { get; set; } = new List<UserTenantRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
