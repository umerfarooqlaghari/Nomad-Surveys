using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class UserTenantRole
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public Guid RoleId { get; set; }
    
    public Guid? TenantId { get; set; } // Nullable for global roles
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual TenantRole Role { get; set; } = null!;
    public virtual Tenant? Tenant { get; set; }
}
