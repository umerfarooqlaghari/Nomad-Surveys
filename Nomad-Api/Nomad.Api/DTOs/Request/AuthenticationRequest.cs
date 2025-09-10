using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

public class TenantRequiredForNonSuperAdminAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        // This will be validated in the service layer instead
        return true;
    }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public string TenantSlug { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; } = false;
}

public class SuperAdminLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

public class CreateUserRequest
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
    
    public List<string> Roles { get; set; } = new();
}

public class AssignRoleRequest
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public string RoleName { get; set; } = string.Empty;
    
    public DateTime? ExpiresAt { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
