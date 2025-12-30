using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

public class CreateTenantRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
    public string Slug { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public CreateCompanyRequest Company { get; set; } = null!;
    
    public CreateTenantAdminRequest? TenantAdmin { get; set; }
}

public class CreateCompanyRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [Range(1, int.MaxValue)]
    public int? NumberOfEmployees { get; set; }
    
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Location { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Industry { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string ContactPersonName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string ContactPersonEmail { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? ContactPersonRole { get; set; }
    
    [Phone]
    [StringLength(20)]
    public string? ContactPersonPhone { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }
}

public class CreateTenantAdminRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? Password { get; set; }
}

public class UpdateTenantAdminRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    // Phone number and password are NOT included in updates
    // They should be updated through separate endpoints
    // All fields are nullable since tenant admin details are not editable in the UI
}

public class UpdateTenantRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public CreateCompanyRequest Company { get; set; } = null!;

    // TenantAdmin is nullable since admin details are not editable in the UI during updates
    public UpdateTenantAdminRequest? TenantAdmin { get; set; }
}
