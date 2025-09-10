using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty; // URL-friendly identifier
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<TenantRole> TenantRoles { get; set; } = new List<TenantRole>();
    public virtual Company? Company { get; set; }
}
