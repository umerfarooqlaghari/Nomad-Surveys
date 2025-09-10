namespace Nomad.Api.Domain.Models;

public class RoleDomain
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? TenantId { get; set; }
    
    // Related data
    public TenantDomain? Tenant { get; set; }
    public List<PermissionDomain> Permissions { get; set; } = new();
}
