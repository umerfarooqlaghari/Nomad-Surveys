using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

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
    
    public string? NumberOfEmployees { get; set; }
    
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

public class UpdateTenantAdminRequest : IValidatableObject
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

    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    public System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        var fields = new[] { FirstName, LastName, Email, PhoneNumber, Password };
        var anySet = fields.Any(f => !string.IsNullOrWhiteSpace(f));
        var allSet = fields.All(f => !string.IsNullOrWhiteSpace(f));

        if (anySet && !allSet)
        {
            yield return new System.ComponentModel.DataAnnotations.ValidationResult(
                "If any admin field is provided, all admin fields (First Name, Last Name, Email, Phone Number, Password) must be provided.",
                new[] { nameof(FirstName), nameof(LastName), nameof(Email), nameof(PhoneNumber), nameof(Password) }
            );
        }
    }
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
