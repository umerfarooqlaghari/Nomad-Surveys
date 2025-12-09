using Nomad.Api.DTOs.Common;

namespace Nomad.Api.DTOs.Response;

public class TenantSettingsResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string DefaultQuestionType { get; set; } = "rating";
    public List<RatingOptionDto>? DefaultRatingOptions { get; set; }
    public int? NumberOfOptions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

