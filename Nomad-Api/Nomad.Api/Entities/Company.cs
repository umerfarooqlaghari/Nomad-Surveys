using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class Company
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Range(1, int.MaxValue)]
    public int NumberOfEmployees { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Industry { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ContactPersonName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string ContactPersonEmail { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ContactPersonRole { get; set; } = string.Empty;
    
    [Phone]
    [MaxLength(20)]
    public string? ContactPersonPhone { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign key
    public Guid TenantId { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ApplicationUser? ContactPerson { get; set; }
    public Guid? ContactPersonId { get; set; }
}
