using System.Text.Json;

namespace Nomad.Api.DTOs.Request;

/// <summary>
/// Request DTO for saving draft response
/// </summary>
public class SaveDraftRequest
{
    public JsonDocument ResponseData { get; set; } = null!;
}

