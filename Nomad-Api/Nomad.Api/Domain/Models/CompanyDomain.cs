namespace Nomad.Api.Domain.Models;

public class CompanyDomain
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NumberOfEmployees { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string ContactPersonEmail { get; set; } = string.Empty;
    public string? ContactPersonRole { get; set; } = string.Empty;
    public string? ContactPersonPhone { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ContactPersonId { get; set; }
    
    // Related data
    public TenantDomain? Tenant { get; set; }
    public UserDomain? ContactPerson { get; set; }
}
