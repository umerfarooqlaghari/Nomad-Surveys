using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

/// <summary>
/// Request DTO for creating a new survey
/// </summary>
public class CreateSurveyRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// SurveyJS JSON schema object
    /// Can include dynamic placeholders like {subjectName}, {evaluatorName}
    /// </summary>
    [Required]
    public object Schema { get; set; } = new { };
}

/// <summary>
/// Request DTO for updating an existing survey
/// </summary>
public class UpdateSurveyRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// SurveyJS JSON schema object
    /// Can include dynamic placeholders like {subjectName}, {evaluatorName}
    /// </summary>
    [Required]
    public object Schema { get; set; } = new { };
    
    public bool? IsActive { get; set; }
}

