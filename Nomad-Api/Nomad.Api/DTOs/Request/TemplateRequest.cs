using System.Text.Json;

namespace Nomad.Api.DTOs.Request;

/// <summary>
/// Request to create a report template
/// </summary>
public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public JsonDocument TemplateSchema { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument? PlaceholderMappings { get; set; }
}

/// <summary>
/// Request to update a report template
/// </summary>
public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public JsonDocument? TemplateSchema { get; set; }
    public JsonDocument? PlaceholderMappings { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to generate a report from a template with data
/// </summary>
public class GenerateReportRequest
{
    public Guid TemplateId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? SurveyId { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; } // For custom data injection
}


