namespace Nomad.Api.Domain.Models;

public class UserRoleDomain
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? TenantId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    
    // Related data
    public UserDomain? User { get; set; }
    public RoleDomain? Role { get; set; }
    public TenantDomain? Tenant { get; set; }
}
