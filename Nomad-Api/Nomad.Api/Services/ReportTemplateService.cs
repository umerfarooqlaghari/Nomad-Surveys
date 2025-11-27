using System.Text;
using System.Text.RegularExpressions;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using PuppeteerSharp.Media;


namespace Nomad.Api.Services;

public class ReportTemplateService : IReportTemplateService
{
    private readonly IReportingService _reportingService;
    private readonly IReportTemplateSettingsService? _templateSettingsService;
    private readonly ILogger<ReportTemplateService> _logger;
    private readonly string _templatePath;

    public ReportTemplateService(
        IReportingService reportingService,
        ILogger<ReportTemplateService> logger,
        IWebHostEnvironment environment,
        IServiceProvider serviceProvider)
    {
        _reportingService = reportingService;
        _logger = logger;
        _templatePath = Path.Combine(environment.ContentRootPath, "Templates", "ReportTemplate.html");
        
        // Get template settings service if available (optional dependency)
        try
        {
            _templateSettingsService = serviceProvider.GetService<IReportTemplateSettingsService>();
        }
        catch
        {
            _templateSettingsService = null;
        }
    }

    public async Task<string> GeneratePreviewHtmlAsync(
        string companyName,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null)
    {
        try
        {
            // Check if template file exists
            if (!File.Exists(_templatePath))
            {
                _logger.LogError("Template file not found at path: {TemplatePath}", _templatePath);
                throw new FileNotFoundException($"Template file not found at path: {_templatePath}");
            }

            // Load HTML template
            var htmlTemplate = await File.ReadAllTextAsync(_templatePath);

            if (string.IsNullOrWhiteSpace(htmlTemplate))
            {
                _logger.LogError("Template file is empty at path: {TemplatePath}", _templatePath);
                throw new InvalidOperationException($"Template file is empty at path: {_templatePath}");
            }

            // Use mock data for preview
            var html = ReplacePlaceholdersForPreview(htmlTemplate, companyName, companyLogoUrl, coverImageUrl, primaryColor, secondaryColor, tertiaryColor);

            return html;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Template file not found");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview HTML. Template path: {TemplatePath}", _templatePath);
            throw;
        }
    }

    public async Task<string> GenerateReportHtmlAsync(
        Guid subjectId,
        Guid? surveyId,
        Guid tenantId,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null)
    {
        try
        {
            // Load HTML template
            var htmlTemplate = await File.ReadAllTextAsync(_templatePath);

            // Fetch comprehensive report data
            var reportData = await _reportingService.GetComprehensiveReportAsync(
                subjectId, surveyId, tenantId);

            if (reportData == null)
            {
                throw new InvalidOperationException($"No report data found for subject {subjectId}");
            }

            // Replace placeholders with actual data
            var html = ReplacePlaceholders(htmlTemplate, reportData, companyLogoUrl, coverImageUrl, primaryColor, secondaryColor, tertiaryColor);

            // Save template settings if provided
            await SaveTemplateSettingsIfProvided(
                tenantId,
                companyLogoUrl,
                coverImageUrl,
                primaryColor,
                secondaryColor,
                tertiaryColor);

            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report HTML for subject {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<byte[]> GenerateReportPdfAsync(
        Guid subjectId,
        Guid? surveyId,
        Guid tenantId,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null)
    {
        try
        {
            // Generate HTML first
            var html = await GenerateReportHtmlAsync(
                subjectId, surveyId, tenantId, companyLogoUrl, coverImageUrl, primaryColor, secondaryColor, tertiaryColor);

            // Convert HTML to PDF using PuppeteerSharp
            var pdfBytes = await GeneratePdfFromHtmlAsync(html);
            
            // Save template settings if provided
            await SaveTemplateSettingsIfProvided(
                tenantId,
                companyLogoUrl,
                coverImageUrl,
                primaryColor,
                secondaryColor,
                tertiaryColor);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report PDF for subject {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<byte[]> GenerateChartImageAsync(
        string chartType,
        Dictionary<string, object> chartData,
        int width = 800,
        int height = 600)
    {
        // This is a placeholder - replace with real chart rendering when integrated
        var info = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // Clear canvas with white background
        canvas.Clear(SKColors.White);

        // Draw placeholder centered text using SKFont (modern API)
        using var paint = new SKPaint
        {
            Color = SKColors.Blue,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var font = new SKFont(SKTypeface.Default, 48);
        var text = $"{chartType} Chart";
        // Measure text width and use font metrics for vertical centering
        var textWidth = font.MeasureText(text, paint);
        var metrics = font.Metrics;
        var centerX = width / 2f;
        var centerY = height / 2f - (metrics.Ascent + metrics.Descent) / 2f;

        canvas.DrawText(text, centerX, centerY, SKTextAlign.Center, font, paint);

        // Get image bytes
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private string ReplacePlaceholdersForPreview(
        string template,
        string companyName,
        string? companyLogoUrl,
        string? coverImageUrl,
        string? primaryColor,
        string? secondaryColor,
        string? tertiaryColor)
    {
        var html = template;

        // Replace brand colors
        html = html.Replace("{{PRIMARY_COLOR}}", primaryColor ?? "#0455A4");
        html = html.Replace("{{SECONDARY_COLOR}}", secondaryColor ?? "#1D8F6C");
        html = html.Replace("{{TERTIARY_COLOR}}", tertiaryColor ?? "#6C757D");

        // Replace company information
        html = html.Replace("{{COMPANY_NAME}}", companyName ?? "Company Name");
        
        // Replace logo in header
        if (!string.IsNullOrEmpty(companyLogoUrl))
        {
            html = html.Replace("{{COMPANY_LOGO}}", 
                $"<img src=\"{companyLogoUrl}\" alt=\"Company Logo\" class=\"client-logo\" />");
        }
        else
        {
            html = html.Replace("{{COMPANY_LOGO}}", 
                "<div class=\"client-logo\" style=\"width: 80mm; height: 30mm; background: #f0f0f0; border: 1px solid #ddd; display: flex; align-items: center; justify-content: center; font-size: 10pt; color: #999;\">CLIENT LOGO</div>");
        }

        // Mock subject information for preview
        html = html.Replace("{{SUBJECT_TITLE}}", "Mr.");
        html = html.Replace("{{SUBJECT_NAME}}", "Umer Farooq");
        html = html.Replace("{{SUBJECT_DEPARTMENT}}", "Sales Department");
        html = html.Replace("{{SUBJECT_POSITION}}", "Senior Sales Executive");
        html = html.Replace("{{SUBJECT_EMPLOYEE_ID}}", "EMP-12345");
        
        // Cover image
        if (!string.IsNullOrEmpty(coverImageUrl))
        {
            html = html.Replace("{{COVER_IMAGE}}", 
                $"<img src=\"{coverImageUrl}\" alt=\"Cover Image\" class=\"cover-image\" style=\"width: 100%; height: auto; object-fit: cover;\" />");
        }
        else
        {
            html = html.Replace("{{COVER_IMAGE}}", 
                "<div class=\"cover-image\" style=\"width: 100%; height: 200mm; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); display: flex; align-items: center; justify-content: center; color: white; font-size: 24pt; font-weight: bold;\">Cover Image Placeholder</div>");
        }

        // Replace dates
        var now = DateTime.Now;
        html = html.Replace("{{REPORT_DATE}}", now.ToString("dd MMMM, yyyy"));
        html = html.Replace("{{REPORT_YEAR}}", now.Year.ToString());
        html = html.Replace("{{REPORT_PERIOD}}", now.ToString("MMMM yyyy"));
        html = html.Replace("{{GENERATION_DATE}}", now.ToString("dd MMMM, yyyy HH:mm"));

        // Mock scores for preview
        html = html.Replace("{{OVERALL_SCORE}}", "85.5");
        html = html.Replace("{{OVERALL_RATING}}", "Very Good");
        html = html.Replace("{{SELF_SCORE}}", "88.0");
        html = html.Replace("{{SELF_RATING}}", "Excellent");
        html = html.Replace("{{EVALUATOR_SCORE}}", "83.0");
        html = html.Replace("{{EVALUATOR_RATING}}", "Very Good");
        html = html.Replace("{{ORGANIZATION_COMPARISON}}", "+5.2");
        html = html.Replace("{{ORGANIZATION_RATING}}", "Above Average");

        html = html.Replace("{{SELF_AVERAGE_SCORE}}", "88.0");
        html = html.Replace("{{EVALUATOR_AVERAGE_SCORE}}", "83.0");

        // Replace performance indicators
        html = html.Replace("{{SATISFACTION_SCORE}}", "85.5");
        html = html.Replace("{{IMPROVEMENT_SCORE}}", "75.0");

        // Replace charts (placeholder boxes)
        html = html.Replace("{{CHART_OVERALL_SATISFACTION}}", 
            "<div class=\"chart-placeholder\">Overall Satisfaction Chart - Chart will be rendered here</div>");
        html = html.Replace("{{CHART_QUESTION_PERFORMANCE}}", 
            "<div class=\"chart-placeholder\">Question Performance Chart - Chart will be rendered here</div>");
        html = html.Replace("{{CHART_SELF_VS_EVALUATOR}}", 
            "<div class=\"chart-placeholder\">Self vs Evaluator Comparison Chart - Chart will be rendered here</div>");

        // Replace questions table with mock data
        html = html.Replace("{{QUESTIONS_TABLE}}", GenerateMockQuestionsTable());

        return html;
    }

    private string ReplacePlaceholders(
        string template,
        ComprehensiveReportResponse reportData,
        string? companyLogoUrl,
        string? coverImageUrl,
        string? primaryColor,
        string? secondaryColor,
        string? tertiaryColor)
    {
        var html = template;

        // Replace brand colors
        html = html.Replace("{{PRIMARY_COLOR}}", primaryColor ?? "#0455A4");
        html = html.Replace("{{SECONDARY_COLOR}}", secondaryColor ?? "#1D8F6C");
        html = html.Replace("{{TERTIARY_COLOR}}", tertiaryColor ?? "#6C757D");

        // Replace company information (use companyName parameter if available)
        // This will be passed through the GenerateReportHtmlAsync method signature
        html = html.Replace("{{COMPANY_NAME}}", "Company Name"); // TODO: Get from tenant settings or request
        
        // Replace logo in header
        if (!string.IsNullOrEmpty(companyLogoUrl))
        {
            html = html.Replace("{{COMPANY_LOGO}}", 
                $"<img src=\"{companyLogoUrl}\" alt=\"Company Logo\" class=\"client-logo\" />");
        }
        else
        {
            html = html.Replace("{{COMPANY_LOGO}}", 
                "<div class=\"client-logo\" style=\"width: 80mm; height: 30mm; background: #f0f0f0; border: 1px solid #ddd; display: flex; align-items: center; justify-content: center; font-size: 10pt; color: #999;\">CLIENT LOGO</div>");
        }
        
        // Cover image
        if (!string.IsNullOrEmpty(coverImageUrl))
        {
            html = html.Replace("{{COVER_IMAGE}}", 
                $"<img src=\"{coverImageUrl}\" alt=\"Cover Image\" class=\"cover-image\" style=\"width: 100%; height: auto; object-fit: cover;\" />");
        }
        else
        {
            html = html.Replace("{{COVER_IMAGE}}", 
                "<div class=\"cover-image\" style=\"width: 100%; height: 200mm; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); display: flex; align-items: center; justify-content: center; color: white; font-size: 24pt; font-weight: bold;\">Cover Image</div>");
        }

        // Replace subject information
        html = html.Replace("{{SUBJECT_TITLE}}", "Mr."); // TODO: Determine from gender or user preference
        html = html.Replace("{{SUBJECT_NAME}}", reportData.SubjectName ?? "N/A");
        html = html.Replace("{{SUBJECT_DEPARTMENT}}", "N/A"); // TODO: Get from subject entity
        html = html.Replace("{{SUBJECT_POSITION}}", "N/A"); // TODO: Get from subject entity
        html = html.Replace("{{SUBJECT_EMPLOYEE_ID}}", reportData.SubjectId.ToString());

        // Replace dates
        var now = DateTime.Now;
        html = html.Replace("{{REPORT_DATE}}", now.ToString("dd MMMM, yyyy"));
        html = html.Replace("{{REPORT_YEAR}}", now.Year.ToString());
        html = html.Replace("{{REPORT_PERIOD}}", now.ToString("MMMM yyyy"));
        html = html.Replace("{{GENERATION_DATE}}", now.ToString("dd MMMM, yyyy HH:mm"));

        // Replace scores (with rating mapping)
        var overallScore = reportData.EvaluatorAverage?.OverallScore ?? reportData.SelfEvaluation?.OverallScore ?? 0;
        html = html.Replace("{{OVERALL_SCORE}}", FormatScore(overallScore));
        html = html.Replace("{{OVERALL_RATING}}", GetRating(overallScore));

        var selfScore = reportData.SelfEvaluation?.OverallScore ?? 0;
        html = html.Replace("{{SELF_SCORE}}", FormatScore(selfScore));
        html = html.Replace("{{SELF_RATING}}", GetRating(selfScore));

        var evaluatorScore = reportData.EvaluatorAverage?.OverallScore ?? 0;
        html = html.Replace("{{EVALUATOR_SCORE}}", FormatScore(evaluatorScore));
        html = html.Replace("{{EVALUATOR_RATING}}", GetRating(evaluatorScore));

        html = html.Replace("{{SELF_AVERAGE_SCORE}}", FormatScore(selfScore));
        html = html.Replace("{{EVALUATOR_AVERAGE_SCORE}}", FormatScore(evaluatorScore));

        // Replace performance indicators
        html = html.Replace("{{SATISFACTION_SCORE}}", FormatScore(overallScore));
        html = html.Replace("{{IMPROVEMENT_SCORE}}", 
            FormatScore(CalculateImprovementScore(reportData)));
        html = html.Replace("{{ORGANIZATION_COMPARISON}}", 
            FormatScore(reportData.SubjectVsOrganizationDifference));
        html = html.Replace("{{ORGANIZATION_RATING}}", 
            reportData.PerformanceLevel == PerformanceLevel.AbovePar ? "Above Average" : 
            reportData.PerformanceLevel == PerformanceLevel.BelowPar ? "Below Average" : "Average");

        // Replace charts (placeholder - will be replaced with actual chart images)
        html = html.Replace("{{CHART_OVERALL_SATISFACTION}}", 
            "<div class=\"chart-placeholder\">Overall Satisfaction Chart - Chart will be rendered here</div>");
        html = html.Replace("{{CHART_QUESTION_PERFORMANCE}}", 
            "<div class=\"chart-placeholder\">Question Performance Chart - Chart will be rendered here</div>");
        html = html.Replace("{{CHART_SELF_VS_EVALUATOR}}", 
            "<div class=\"chart-placeholder\">Self vs Evaluator Comparison Chart - Chart will be rendered here</div>");

        // Replace questions table
        html = html.Replace("{{QUESTIONS_TABLE}}", GenerateQuestionsTable(reportData));

        return html;
    }

    private string GenerateMockQuestionsTable()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"breakdown-table\">");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Question</th>");
        sb.AppendLine("<th>Your Score</th>");
        sb.AppendLine("<th>Evaluator Average</th>");
        sb.AppendLine("<th>Rating</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");

        // Mock questions
        var mockQuestions = new[]
        {
            new { Text = "Communication Skills", SelfScore = 90.0, EvalScore = 85.0 },
            new { Text = "Problem Solving", SelfScore = 88.0, EvalScore = 82.0 },
            new { Text = "Team Collaboration", SelfScore = 85.0, EvalScore = 88.0 },
            new { Text = "Leadership", SelfScore = 87.0, EvalScore = 80.0 },
            new { Text = "Time Management", SelfScore = 82.0, EvalScore = 85.0 },
        };

        foreach (var question in mockQuestions)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{EscapeHtml(question.Text)}</td>");
            sb.AppendLine($"<td class=\"score-cell\">{FormatScore(question.SelfScore)}</td>");
            sb.AppendLine($"<td class=\"score-cell\">{FormatScore(question.EvalScore)}</td>");
            sb.AppendLine($"<td><span class=\"rating-badge\">{GetRating(question.EvalScore)}</span></td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        return sb.ToString();
    }

    private string GenerateQuestionsTable(ComprehensiveReportResponse reportData)
    {
        var questions = reportData.SelfVsEvaluatorQuestions ?? new List<QuestionComparisonDto>();
        
        if (!questions.Any())
        {
            return GenerateMockQuestionsTable();
        }

        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"breakdown-table\">");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Question</th>");
        sb.AppendLine("<th>Your Score</th>");
        sb.AppendLine("<th>Evaluator Average</th>");
        sb.AppendLine("<th>Rating</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");

        foreach (var question in questions)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{EscapeHtml(question.QuestionText ?? "N/A")}</td>");
            sb.AppendLine($"<td class=\"score-cell\">{FormatScore(question.SelfScore)}</td>");
            sb.AppendLine($"<td class=\"score-cell\">{FormatScore(question.EvaluatorAverageScore)}</td>");
            sb.AppendLine($"<td><span class=\"rating-badge\">{GetRating(question.EvaluatorAverageScore)}</span></td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        return sb.ToString();
    }

    private async Task<byte[]> GeneratePdfFromHtmlAsync(string html)
    {
        try
        {
            // Use PuppeteerSharp for HTML to PDF conversion
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

await using var page = await browser.NewPageAsync();
// Set page content (removed unsupported WaitUntilNavigation member)
await page.SetContentAsync(html);
var pdfBytes = await page.PdfDataAsync(new PdfOptions
{
    Format = PaperFormat.A4,
    PrintBackground = true,
    MarginOptions = new MarginOptions
    {
        Top = "0",
        Right = "0",
        Bottom = "0",
        Left = "0"
    }
});
    

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF from HTML using PuppeteerSharp. Falling back to simple PDF.");
            
            // Fallback to simple PDF if PuppeteerSharp fails
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            column.Item().Text("Report Generated Successfully")
                                .FontSize(16)
                                .Bold();
                            column.Item().PaddingTop(10);
                            column.Item().Text("The HTML version of this report is available.")
                                .FontSize(12);
                            column.Item().PaddingTop(5);
                            column.Item().Text("PDF generation encountered an error. Please use the HTML export option.")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                        });
                });
            })
            .GeneratePdf();
        }
    }

    private string FormatScore(double? score)
    {
        return score?.ToString("F1") ?? "0.0";
    }

    private string GetRating(double score)
    {
        return score switch
        {
            >= 90 => "Excellent",
            >= 80 => "Very Good",
            >= 70 => "Good",
            >= 60 => "Average",
            >= 50 => "Below Average",
            _ => "Needs Improvement"
        };
    }

    private double CalculateImprovementScore(ComprehensiveReportResponse reportData)
    {
        // Calculate improvement score based on self vs evaluator comparison
        var selfScore = reportData.SelfEvaluation?.OverallScore ?? 0;
        var evaluatorScore = reportData.EvaluatorAverage?.OverallScore ?? 0;
        var difference = evaluatorScore - selfScore;
        // Positive difference means evaluators rated higher, which could indicate improvement potential
        return Math.Max(0, Math.Min(100, 50 + (difference * 0.5))); // Normalize to 0-100 scale
    }

    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#039;");
    }

    private async Task SaveTemplateSettingsIfProvided(
        Guid tenantId,
        string? companyLogoUrl,
        string? coverImageUrl,
        string? primaryColor,
        string? secondaryColor,
        string? tertiaryColor)
    {
        if (_templateSettingsService == null)
        {
            return;
        }

        try
        {
            // Check if any settings are provided
            if (string.IsNullOrEmpty(companyLogoUrl) && 
                string.IsNullOrEmpty(coverImageUrl) && 
                string.IsNullOrEmpty(primaryColor) && 
                string.IsNullOrEmpty(secondaryColor) && 
                string.IsNullOrEmpty(tertiaryColor))
            {
                return; // No settings to save
            }

            // Check if default template exists
            var defaultTemplate = await _templateSettingsService.GetDefaultTemplateSettingsAsync(tenantId);
            
            if (defaultTemplate != null)
            {
                // Update existing default template
                var updateRequest = new UpdateReportTemplateSettingsRequest
                {
                    CompanyLogoUrl = companyLogoUrl,
                    CoverImageUrl = coverImageUrl,
                    PrimaryColor = primaryColor,
                    SecondaryColor = secondaryColor,
                    TertiaryColor = tertiaryColor
                };

                await _templateSettingsService.UpdateTemplateSettingsAsync(
                    defaultTemplate.Id, 
                    updateRequest, 
                    tenantId);
            }
            else
            {
                // Create new template settings
                var createRequest = new CreateReportTemplateSettingsRequest
                {
                    Name = "Default Report Template",
                    Description = "Auto-generated from report creation",
                    CompanyLogoUrl = companyLogoUrl,
                    CoverImageUrl = coverImageUrl,
                    PrimaryColor = primaryColor,
                    SecondaryColor = secondaryColor,
                    TertiaryColor = tertiaryColor,
                    IsDefault = true
                };

                await _templateSettingsService.CreateTemplateSettingsAsync(createRequest, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save template settings for tenant {TenantId}", tenantId);
            // Don't throw - template saving is optional
        }
    }

    // Expose a method to save template HTML back to disk
    public async Task SaveTemplateHtmlAsync(string html)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException("Template HTML cannot be empty", nameof(html));
            }

            var dir = Path.GetDirectoryName(_templatePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            await File.WriteAllTextAsync(_templatePath, html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save template HTML to path {Path}", _templatePath);
            throw;
        }
    }

    // Save tenant-specific template HTML to Templates/{tenant}/ReportTemplate.html
    public async Task SaveTemplateHtmlAsync(string html, string tenant)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException("Template HTML cannot be empty", nameof(html));
            }

            var tenantDir = Path.Combine(Path.GetDirectoryName(_templatePath) ?? "", tenant ?? "");
            if (!Directory.Exists(tenantDir)) Directory.CreateDirectory(tenantDir);

            var tenantPath = Path.Combine(tenantDir, "ReportTemplate.html");
            await File.WriteAllTextAsync(tenantPath, html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save tenant template HTML for tenant {Tenant} to disk", tenant);
            throw;
        }
    }
}

