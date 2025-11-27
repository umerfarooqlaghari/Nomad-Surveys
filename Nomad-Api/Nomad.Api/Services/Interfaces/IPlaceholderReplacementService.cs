using System.Text.Json;

namespace Nomad.Api.Services.Interfaces;

/// <summary>
/// Service for replacing placeholders in templates with actual data
/// </summary>
public interface IPlaceholderReplacementService
{
    /// <summary>
    /// Replace placeholders in template JSON with data from report
    /// </summary>
    Task<JsonDocument> ReplacePlaceholdersAsync(
        JsonDocument templateSchema,
        DTOs.Response.ComprehensiveReportResponse reportData,
        Dictionary<string, object> additionalData);
}


