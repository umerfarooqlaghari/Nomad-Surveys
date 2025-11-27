using System.Text.Json;

namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response for report template
/// </summary>
public class TemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public JsonDocument TemplateSchema { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument? PlaceholderMappings { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>
/// Lightweight template list response
/// </summary>
public class TemplateListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response for report generation
/// </summary>
public class GeneratedReportResponse
{
    public byte[] PdfContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/pdf";
    public string FileName { get; set; } = "report.pdf";
}


