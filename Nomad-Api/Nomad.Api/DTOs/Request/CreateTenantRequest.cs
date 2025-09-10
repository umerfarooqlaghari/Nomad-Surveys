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
    
    [Required]
    public CreateTenantAdminRequest TenantAdmin { get; set; } = null!;
}

public class CreateCompanyRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Range(1, int.MaxValue)]
    public int NumberOfEmployees { get; set; }
    
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
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string ContactPersonRole { get; set; } = string.Empty;
    
    [Phone]
    [StringLength(20)]
    public string? ContactPersonPhone { get; set; }
}

public class CreateTenantAdminRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}
