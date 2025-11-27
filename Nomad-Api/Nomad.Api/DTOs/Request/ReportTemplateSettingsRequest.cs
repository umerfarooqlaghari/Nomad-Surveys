using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

public class CreateReportTemplateSettingsRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(1000)]
    public string? CompanyLogoUrl { get; set; }

    [MaxLength(1000)]
    public string? CoverImageUrl { get; set; }

    [MaxLength(7)]
    public string? PrimaryColor { get; set; }

    [MaxLength(7)]
    public string? SecondaryColor { get; set; }

    [MaxLength(7)]
    public string? TertiaryColor { get; set; }

    public bool IsDefault { get; set; } = false;
}

public class UpdateReportTemplateSettingsRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(1000)]
    public string? CompanyLogoUrl { get; set; }

    [MaxLength(1000)]
    public string? CoverImageUrl { get; set; }

    [MaxLength(7)]
    public string? PrimaryColor { get; set; }

    [MaxLength(7)]
    public string? SecondaryColor { get; set; }

    [MaxLength(7)]
    public string? TertiaryColor { get; set; }

    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
}


