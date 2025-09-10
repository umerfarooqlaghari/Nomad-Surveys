namespace Nomad.Api.Domain.Models;

public class UserDomain
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Guid? TenantId { get; set; }
    
    // Related data
    public TenantDomain? Tenant { get; set; }
    public List<UserRoleDomain> UserRoles { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
