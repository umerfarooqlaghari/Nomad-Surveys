namespace Nomad.Api.Entities;

public class RolePermission
{
    public Guid Id { get; set; }
    
    public Guid RoleId { get; set; }
    
    public Guid PermissionId { get; set; }
    
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual TenantRole Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
