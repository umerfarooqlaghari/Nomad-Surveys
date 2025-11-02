using System.Text.Json;

namespace Nomad.Api.DTOs.Request;

/// <summary>
/// Request DTO for submitting completed evaluation
/// </summary>
public class SubmitEvaluationRequest
{
    public JsonDocument ResponseData { get; set; } = null!;
}

