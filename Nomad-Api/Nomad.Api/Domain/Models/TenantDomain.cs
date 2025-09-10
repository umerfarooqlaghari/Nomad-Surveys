namespace Nomad.Api.Domain.Models;

public class TenantDomain
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Related data
    public CompanyDomain? Company { get; set; }
    public List<UserDomain> Users { get; set; } = new();
    public List<RoleDomain> Roles { get; set; } = new();
}
