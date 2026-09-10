using System.ComponentModel.DataAnnotations;
using Alpha.Api.DTOs.Common;

namespace Alpha.Api.DTOs.Request;

public class CreateTenantSettingsRequest
{
    [Required]
    [MaxLength(50)]
    public string DefaultQuestionType { get; set; } = "rating";

    public List<RatingOptionDto>? DefaultRatingOptions { get; set; }

    public int? NumberOfOptions { get; set; }
}

public class UpdateTenantSettingsRequest
{
    [Required]
    [MaxLength(50)]
    public string DefaultQuestionType { get; set; } = "rating";

    public List<RatingOptionDto>? DefaultRatingOptions { get; set; }

    public int? NumberOfOptions { get; set; }
}

