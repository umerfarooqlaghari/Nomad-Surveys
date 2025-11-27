using Nomad.Api.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace Nomad.Api.Services;

/// <summary>
/// Service for generating PDFs from processed templates using QuestPDF
/// </summary>
public class PdfGenerationService : IPdfGenerationService
{
    private readonly ILogger<PdfGenerationService> _logger;

    public PdfGenerationService(ILogger<PdfGenerationService> logger)
    {
        _logger = logger;
        
        // Set QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePdfAsync(JsonDocument processedTemplate, Guid tenantId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var root = processedTemplate.RootElement;

                // Extract page settings
                var pageSettings = root.TryGetProperty("page", out var pageElement)
                    ? pageElement
                    : JsonDocument.Parse("{}").RootElement;

                var pageSize = pageSettings.TryGetProperty("size", out var sizeElement)
                    ? sizeElement.GetString() ?? "A4"
                    : "A4";

                var orientation = pageSettings.TryGetProperty("orientation", out var orientationElement)
                    ? orientationElement.GetString() ?? "portrait"
                    : "portrait";

                // Extract theme settings
                var theme = root.TryGetProperty("theme", out var themeElement)
                    ? themeElement
                    : JsonDocument.Parse("{}").RootElement;

                var primaryColor = theme.TryGetProperty("primary", out var primaryElement)
                    ? primaryElement.GetString() ?? "#0455A4"
                    : "#0455A4";

                var secondaryColor = theme.TryGetProperty("secondary", out var secondaryElement)
                    ? secondaryElement.GetString() ?? "#1D8F6C"
                    : "#1D8F6C";

                var fontFamily = theme.TryGetProperty("font", out var fontElement)
                    ? fontElement.GetString() ?? "Arial"
                    : "Arial";

                // Extract elements
                var elements = root.TryGetProperty("elements", out var elementsElement)
                    ? elementsElement.EnumerateArray().ToList()
                    : new List<JsonElement>();

                // Create PDF document
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Set page size and orientation
                        var size = GetPageSize(pageSize);
                        page.Size(size);
                        page.Margin(50);

                        // Set page orientation
                        if (orientation == "landscape")
                        {
                            page.Size(size);
                        }

                        // Set default text style
                        page.DefaultTextStyle(style => style
                            .FontFamily(fontFamily)
                            .FontSize(12)
                            .FontColor(primaryColor));

                        // Add elements
                        page.Content().Column(column =>
                        {
                            foreach (var element in elements)
                            {
                                RenderElement(element, column, primaryColor, secondaryColor, fontFamily);
                            }
                        });
                    });
                });

                // Generate PDF bytes
                using var stream = new MemoryStream();
                document.GeneratePdf(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF from template");
                throw;
            }
        });
    }

    private void RenderElement(JsonElement element, ColumnDescriptor column, string primaryColor, string secondaryColor, string fontFamily)
    {
        var elementType = element.TryGetProperty("type", out var typeElement)
            ? typeElement.GetString() ?? "text"
            : "text";

        switch (elementType.ToLower())
        {
            case "text":
                RenderTextElement(element, column, primaryColor, fontFamily);
                break;
            case "image":
                RenderImageElement(element, column);
                break;
            case "table":
                RenderTableElement(element, column, primaryColor, fontFamily);
                break;
            case "chart":
                // Charts will be rendered as images or placeholders for now
                RenderChartPlaceholder(element, column);
                break;
            default:
                RenderTextElement(element, column, primaryColor, fontFamily);
                break;
        }
    }

    private void RenderTextElement(JsonElement element, ColumnDescriptor column, string color, string fontFamily)
    {
        var content = element.TryGetProperty("content", out var contentElement)
            ? contentElement.GetString() ?? ""
            : "";

        var style = element.TryGetProperty("style", out var styleElement)
            ? styleElement
            : JsonDocument.Parse("{}").RootElement;

        var fontSize = style.TryGetProperty("fontSize", out var fontSizeElement)
            ? fontSizeElement.GetSingle()
            : 12f;

        var isBold = style.TryGetProperty("bold", out var boldElement) && boldElement.GetBoolean();
        var isItalic = style.TryGetProperty("italic", out var italicElement) && italicElement.GetBoolean();

        column.Item().Text(text =>
        {
            text.DefaultTextStyle(style => style
                .FontFamily(fontFamily)
                .FontSize(fontSize)
                .FontColor(color));

            var span = text.Span(content);
            
            if (isBold)
            {
                span.Bold();
            }

            if (isItalic)
            {
                span.Italic();
            }
        });
    }

    private void RenderImageElement(JsonElement element, ColumnDescriptor column)
    {
        var src = element.TryGetProperty("src", out var srcElement)
            ? srcElement.GetString() ?? ""
            : "";

        if (string.IsNullOrEmpty(src))
        {
            return;
        }

        column.Item().Image(src);
    }

    private void RenderTableElement(JsonElement element, ColumnDescriptor column, string color, string fontFamily)
    {
        var tableData = element.TryGetProperty("data", out var dataElement)
            ? dataElement
            : JsonDocument.Parse("[]").RootElement;

        if (tableData.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        column.Item().Table(table =>
        {
            var rows = tableData.EnumerateArray().ToList();
            
            if (rows.Count == 0)
            {
                return;
            }

            // Render header row (first row)
            table.ColumnsDefinition(columns =>
            {
                var firstRow = rows[0];
                var headerCells = firstRow.EnumerateArray().ToList();
                
                foreach (var _ in headerCells)
                {
                    columns.RelativeColumn();
                }
            });

            // Render header
            table.Header(header =>
            {
                var firstRow = rows[0];
                var headerCells = firstRow.EnumerateArray().ToList();
                
                foreach (var cell in headerCells)
                {
                    var cellText = cell.GetString() ?? "";
                    header.Cell().Element(cell => cell
                        .Background(color)
                        .Padding(10)
                        .Text(cellText)
                            .FontFamily(fontFamily)
                            .FontSize(10)
                            .FontColor(Colors.White)
                            .Bold());
                }
            });

            // Render body rows
            foreach (var row in rows.Skip(1))
            {
                var cells = row.EnumerateArray().ToList();
                
                foreach (var cell in cells)
                {
                    var cellText = cell.GetString() ?? "";
                    table.Cell().Element(cell => cell
                        .Padding(10)
                        .Text(cellText)
                            .FontFamily(fontFamily)
                            .FontSize(10));
                }
            }
        });
    }

    private void RenderChartPlaceholder(JsonElement element, ColumnDescriptor column)
    {
        var chartType = element.TryGetProperty("chartType", out var typeElement)
            ? typeElement.GetString() ?? "bar"
            : "bar";

        column.Item().Text(text =>
        {
            text.Span($"[Chart: {chartType}]");
        });
    }

    private QuestPDF.Helpers.PageSize GetPageSize(string size)
    {
        return size.ToUpper() switch
        {
            "A4" => PageSizes.A4,
            "A3" => PageSizes.A3,
            "A5" => PageSizes.A5,
            "LETTER" => PageSizes.Letter,
            "LEGAL" => PageSizes.Legal,
            _ => PageSizes.A4
        };
    }
}

