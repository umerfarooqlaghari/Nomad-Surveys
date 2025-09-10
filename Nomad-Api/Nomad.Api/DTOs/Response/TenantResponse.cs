namespace Nomad.Api.DTOs.Response;

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public CompanyResponse? Company { get; set; }
}

public class TenantListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public string? CompanyName { get; set; }
}

public class CompanyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NumberOfEmployees { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string ContactPersonEmail { get; set; } = string.Empty;
    public string ContactPersonRole { get; set; } = string.Empty;
    public string? ContactPersonPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    public UserResponse? ContactPerson { get; set; }
}
