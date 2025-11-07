using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

/// <summary>
/// Request DTO for creating a new competency
/// </summary>
public class CreateCompetencyRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public Guid ClusterId { get; set; }
}

/// <summary>
/// Request DTO for updating an existing competency
/// </summary>
public class UpdateCompetencyRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public Guid ClusterId { get; set; }

    public bool? IsActive { get; set; }
}

