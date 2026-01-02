using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

/// <summary>
/// Request DTO for creating a new question
/// </summary>
public class CreateQuestionRequest
{
    [Required]
    public Guid CompetencyId { get; set; }

    [StringLength(2000)]
    public string? SelfQuestion { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string OthersQuestion { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating an existing question
/// </summary>
public class UpdateQuestionRequest
{
    [Required]
    public Guid CompetencyId { get; set; }

    [StringLength(2000)]
    public string? SelfQuestion { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string OthersQuestion { get; set; } = string.Empty;

    public bool? IsActive { get; set; }
}

