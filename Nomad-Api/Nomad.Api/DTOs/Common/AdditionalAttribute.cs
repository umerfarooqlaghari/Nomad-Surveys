using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Common;

public class AdditionalAttribute
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Value { get; set; } = string.Empty;
}

