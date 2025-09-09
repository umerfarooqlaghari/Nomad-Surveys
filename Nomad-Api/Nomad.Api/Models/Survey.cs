using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.Models;

public class Survey
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
}
