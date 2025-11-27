using System.Text.Json;

namespace Nomad.Api.Services.Interfaces;

/// <summary>
/// Service for generating PDFs from processed templates
/// </summary>
public interface IPdfGenerationService
{
    /// <summary>
    /// Generate PDF from processed template JSON
    /// </summary>
    Task<byte[]> GeneratePdfAsync(JsonDocument processedTemplate, Guid tenantId);
}


