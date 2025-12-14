using System.Text;
using System.Text.RegularExpressions;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Repository;
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
    private readonly ISubjectService? _subjectService;
    private readonly ReportAnalyticsRepository? _reportAnalyticsRepository;
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

        // Get subject service if available (optional dependency)
        try
        {
            _subjectService = serviceProvider.GetService<ISubjectService>();
        }
        catch
        {
            _subjectService = null;
        }

        // Get report analytics repository if available (optional dependency)
        try
        {
            _reportAnalyticsRepository = serviceProvider.GetService<ReportAnalyticsRepository>();
        }
        catch
        {
            _reportAnalyticsRepository = null;
        }
    }

      private async Task<string> LoadTemplateAsync()
    {
        // Try multiple paths to find the template file
        var possiblePaths = new List<string>
        {
            _templatePath,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ReportTemplate.html"),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ReportTemplate.html"),
            Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "Templates", "ReportTemplate.html")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _logger.LogInformation("Loading template from: {TemplatePath}", path);
                var content = await File.ReadAllTextAsync(path);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content;
                }
            }
        }

        // If no template found, log all attempted paths
        _logger.LogError("Template file not found. Attempted paths: {Paths}", string.Join(", ", possiblePaths));
        throw new FileNotFoundException($"Template file not found. Attempted paths: {string.Join(", ", possiblePaths)}");
    }

    public async Task<string> GeneratePreviewHtmlAsync(
        string companyName,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null,
        Guid? subjectId = null,
        Guid? surveyId = null,
        Guid? tenantId = null)
    {
        try
        {
            var htmlTemplate = await LoadTemplateAsync();

            // Fetch subject name if subjectId is provided
            string? subjectName = null;
            Guid? subjectTenantId = null;
            if (subjectId.HasValue && _subjectService != null)
            {
                var subject = await _subjectService.GetSubjectByIdAsync(subjectId.Value);
                if (subject?.Employee != null)
                {
                    subjectName = subject.Employee.FullName;
                    subjectTenantId = subject.TenantId;
                }
            }

            // Get self-assessment status and relationship completion stats if subjectId, surveyId, and tenantId are provided
            string selfAssessmentStatus = "Completed"; // Default for preview
            List<RelationshipCompletionStats>? relationshipStats = null;
            HighLowScoresResult? highLowScores = null;
            LatentStrengthsBlindspotsResult? latentStrengthsBlindspots = null;
            var effectiveTenantId = tenantId ?? subjectTenantId;
            if (subjectId.HasValue && surveyId.HasValue && effectiveTenantId.HasValue && _reportAnalyticsRepository != null)
            {
                try
                {
                    selfAssessmentStatus = await _reportAnalyticsRepository.GetSelfAssessmentStatusAsync(
                        subjectId.Value, surveyId.Value, effectiveTenantId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get self-assessment status for preview. Using default value.");
                    selfAssessmentStatus = "Completed"; // Fallback for preview
                }

                try
                {
                    relationshipStats = await _reportAnalyticsRepository.GetRelationshipCompletionStatsAsync(
                        subjectId.Value, surveyId.Value, effectiveTenantId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get relationship completion stats for preview. Using mock data.");
                }

                try
                {
                    highLowScores = await _reportAnalyticsRepository.GetHighLowScoresAsync(
                        subjectId.Value, surveyId.Value, effectiveTenantId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get high/low scores for preview. Using mock data.");
                }

                try
                {
                    latentStrengthsBlindspots = await _reportAnalyticsRepository.GetLatentStrengthsAndBlindspotsAsync(
                        subjectId.Value, surveyId.Value, effectiveTenantId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get latent strengths/blindspots for preview. Using mock data.");
                }
            }

            // Use mock data for preview, but use real subject name if available
            var html = ReplacePlaceholdersForPreview(htmlTemplate, companyName, companyLogoUrl, coverImageUrl, primaryColor, secondaryColor, tertiaryColor, subjectName, selfAssessmentStatus, relationshipStats, highLowScores, latentStrengthsBlindspots);

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
            var htmlTemplate = await LoadTemplateAsync();

            // Fetch comprehensive report data
            var reportData = await _reportingService.GetComprehensiveReportAsync(
                subjectId, surveyId, tenantId);

            if (reportData == null)
            {
                throw new InvalidOperationException($"No report data found for subject {subjectId}");
            }

            // Get self-assessment status, relationship completion stats, high/low scores, and latent strengths/blindspots
            string selfAssessmentStatus = "Not Available";
            List<RelationshipCompletionStats>? relationshipStats = null;
            HighLowScoresResult? highLowScores = null;
            LatentStrengthsBlindspotsResult? latentStrengthsBlindspots = null;
            if (surveyId.HasValue && _reportAnalyticsRepository != null)
            {
                try
                {
                    selfAssessmentStatus = await _reportAnalyticsRepository.GetSelfAssessmentStatusAsync(
                        subjectId, surveyId.Value, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get self-assessment status for subject {SubjectId}", subjectId);
                    selfAssessmentStatus = "Not Available";
                }

                try
                {
                    relationshipStats = await _reportAnalyticsRepository.GetRelationshipCompletionStatsAsync(
                        subjectId, surveyId.Value, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get relationship completion stats for subject {SubjectId}", subjectId);
                }

                try
                {
                    highLowScores = await _reportAnalyticsRepository.GetHighLowScoresAsync(
                        subjectId, surveyId.Value, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get high/low scores for subject {SubjectId}", subjectId);
                }

                try
                {
                    latentStrengthsBlindspots = await _reportAnalyticsRepository.GetLatentStrengthsAndBlindspotsAsync(
                        subjectId, surveyId.Value, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get latent strengths/blindspots for subject {SubjectId}", subjectId);
                }
            }

            // Replace placeholders with actual data
            var html = ReplacePlaceholders(htmlTemplate, reportData, companyLogoUrl, coverImageUrl, primaryColor, secondaryColor, tertiaryColor, selfAssessmentStatus, relationshipStats, highLowScores, latentStrengthsBlindspots);

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
        string? tertiaryColor,
        string? subjectName = null,
        string? selfAssessmentStatus = null,
        List<RelationshipCompletionStats>? relationshipStats = null,
        HighLowScoresResult? highLowScores = null,
        LatentStrengthsBlindspotsResult? latentStrengthsBlindspots = null)
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

        // Subject information for preview - use real name if provided, otherwise mock data
        html = html.Replace("{{SUBJECT_TITLE}}", "Mr.");
        html = html.Replace("{{SUBJECT_NAME}}", subjectName ?? "Umer Farooq");
        html = html.Replace("{{SUBJECT_DEPARTMENT}}", "Sales Department");
        html = html.Replace("{{SUBJECT_POSITION}}", "Senior Sales Executive");
        html = html.Replace("{{SUBJECT_EMPLOYEE_ID}}", "EMP-12345");

        // Replace self-assessment status (computed from ReportAnalyticsRepository)
        html = html.Replace("{{SELF_ASSESSMENT_STATUS}}", selfAssessmentStatus ?? "Completed");

        // Replace relationship completion rows
        html = html.Replace("{{RELATIONSHIP_COMPLETION_ROWS}}", GenerateRelationshipCompletionRows(relationshipStats));

        // Replace high/low scores rows
        html = html.Replace("{{HIGHEST_SCORES_ROWS}}", GenerateHighLowScoresRows(highLowScores?.HighestScores, isHighest: true));
        html = html.Replace("{{LOWEST_SCORES_ROWS}}", GenerateHighLowScoresRows(highLowScores?.LowestScores, isHighest: false));

        // Replace latent strengths and blindspots - use real data if available, otherwise mock data
        var hasLatentStrengthsData = latentStrengthsBlindspots != null &&
            ((latentStrengthsBlindspots.LatentStrengths?.Count > 0) || (latentStrengthsBlindspots.Blindspots?.Count > 0));

        if (hasLatentStrengthsData)
        {
            html = html.Replace("{{LATENT_STRENGTHS_ROWS}}", GenerateGapScoreRows(latentStrengthsBlindspots?.LatentStrengths, isLatentStrength: true));
            html = html.Replace("{{BLINDSPOTS_ROWS}}", GenerateGapScoreRows(latentStrengthsBlindspots?.Blindspots, isLatentStrength: false));
        }
        else
        {
            html = html.Replace("{{LATENT_STRENGTHS_ROWS}}", GenerateMockGapScoreRows(isLatentStrength: true));
            html = html.Replace("{{BLINDSPOTS_ROWS}}", GenerateMockGapScoreRows(isLatentStrength: false));
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

    /// <summary>
    /// Generates HTML table rows for relationship completion statistics
    /// </summary>
    private string GenerateRelationshipCompletionRows(List<RelationshipCompletionStats>? stats)
    {
        if (stats == null || stats.Count == 0)
        {
            // Return mock data if no stats available
            return @"
        <tr>
            <td>Direct Reports</td>
            <td>0 / 0</td>
            <td>0%</td>
        </tr>
        <tr>
            <td>Line-Manager</td>
            <td>0 / 0</td>
            <td>0%</td>
        </tr>
        <tr>
            <td>Peers</td>
            <td>0 / 0</td>
            <td>0%</td>
        </tr>
        <tr>
            <td>Stakeholders</td>
            <td>0 / 0</td>
            <td>0%</td>
        </tr>";
        }

        var sb = new StringBuilder();
        foreach (var stat in stats)
        {
            sb.AppendLine($@"
        <tr>
            <td>{stat.RelationshipType}</td>
            <td>{stat.Completed} / {stat.Total}</td>
            <td>{stat.PercentComplete:0}%</td>
        </tr>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates HTML table rows for highest or lowest scores
    /// </summary>
    private string GenerateHighLowScoresRows(List<HighLowScoreItem>? items, bool isHighest)
    {
        if (items == null || items.Count == 0)
        {
            // Return placeholder row if no data available
            var message = isHighest ? "No high scores available" : "No low scores available";
            return $@"
        <tr>
            <td colspan=""4"" style=""text-align: center; font-style: italic; color: #666;"">{message}</td>
        </tr>";
        }

        var sb = new StringBuilder();
        foreach (var item in items)
        {
            sb.AppendLine($@"
        <tr>
            <td>{item.Rank}</td>
            <td>{System.Web.HttpUtility.HtmlEncode(item.Dimension)}</td>
            <td>{System.Web.HttpUtility.HtmlEncode(item.Item)}</td>
            <td>{item.Average:0.00}</td>
        </tr>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates HTML table rows for latent strengths or blindspots (gap analysis)
    /// </summary>
    private string GenerateGapScoreRows(List<GapScoreItem>? items, bool isLatentStrength)
    {
        if (items == null || items.Count == 0)
        {
            // Return placeholder row if no data available
            var message = isLatentStrength ? "No latent strengths available" : "No blindspots available";
            return $@"
        <tr>
            <td colspan=""6"" style=""text-align: center; font-style: italic; color: #666;"">{message}</td>
        </tr>";
        }

        var sb = new StringBuilder();
        foreach (var item in items)
        {
            // Format gap with sign (+ for positive, - for negative)
            var gapDisplay = item.Gap >= 0 ? $"+{item.Gap:0.00}" : $"{item.Gap:0.00}";
            // Color code: green for positive gap, red for negative gap
            var gapColor = item.Gap >= 0 ? "#1D8F6C" : "#DC3545";

            sb.AppendLine($@"
        <tr>
            <td>{item.Rank}</td>
            <td>{System.Web.HttpUtility.HtmlEncode(item.ScoringCategory)}</td>
            <td>{System.Web.HttpUtility.HtmlEncode(item.Item)}</td>
            <td>{item.Self:0.00}</td>
            <td>{item.Others:0.00}</td>
            <td style=""color: {gapColor}; font-weight: bold;"">{gapDisplay}</td>
        </tr>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates mock gap score rows for preview
    /// </summary>
    private string GenerateMockGapScoreRows(bool isLatentStrength)
    {
        var sb = new StringBuilder();

        if (isLatentStrength)
        {
            // Mock latent strengths (positive gaps)
            var mockItems = new[]
            {
                new { Rank = 1, Category = "Purposeful Conversations", Item = "Demonstrates compassion and respect for others through actions, stays connected about their work and life", Self = 3.00, Others = 3.58, Gap = 0.58 },
                new { Rank = 2, Category = "Decision Making", Item = "Takes a relative view of all factors when making decisions, and sticks through them", Self = 3.00, Others = 3.36, Gap = 0.36 },
                new { Rank = 3, Category = "Network and Influence", Item = "Actively seeks opportunities to build professional and influential contacts, both inside and outside", Self = 3.00, Others = 3.33, Gap = 0.33 },
                new { Rank = 4, Category = "Enterprising Drive", Item = "Sets stretch goals for the organization, linked with the overall vision of the organization", Self = 3.00, Others = 3.30, Gap = 0.30 },
            };

            foreach (var item in mockItems)
            {
                sb.AppendLine($@"
        <tr>
            <td>{item.Rank}</td>
            <td>{item.Category}</td>
            <td>{item.Item}</td>
            <td>{item.Self:0.00}</td>
            <td>{item.Others:0.00}</td>
            <td style=""color: #1D8F6C; font-weight: bold;"">+{item.Gap:0.00}</td>
        </tr>");
            }
        }
        else
        {
            // Mock blindspots (negative gaps)
            var mockItems = new[]
            {
                new { Rank = 1, Category = "Expansive Thinking", Item = "Analyzes a complex situation carefully and reduces it to simple terms", Self = 4.00, Others = 2.73, Gap = -1.27 },
                new { Rank = 2, Category = "Catalytic Learning", Item = "Is open to new ideas that may change own goals for the benefit of the team", Self = 4.00, Others = 2.73, Gap = -1.27 },
                new { Rank = 3, Category = "Inventive Execution", Item = "Quickly masters new functional knowledge necessary to do the job", Self = 4.00, Others = 2.82, Gap = -1.18 },
                new { Rank = 4, Category = "Nurture Growth", Item = "Encourages others to learn from successes and analyze their failures", Self = 4.00, Others = 2.91, Gap = -1.09 },
            };

            foreach (var item in mockItems)
            {
                sb.AppendLine($@"
        <tr>
            <td>{item.Rank}</td>
            <td>{item.Category}</td>
            <td>{item.Item}</td>
            <td>{item.Self:0.00}</td>
            <td>{item.Others:0.00}</td>
            <td style=""color: #DC3545; font-weight: bold;"">{item.Gap:0.00}</td>
        </tr>");
            }
        }

        return sb.ToString();
    }

    private string ReplacePlaceholders(
        string template,
        ComprehensiveReportResponse reportData,
        string? companyLogoUrl,
        string? coverImageUrl,
        string? primaryColor,
        string? secondaryColor,
        string? tertiaryColor,
        string? selfAssessmentStatus = null,
        List<RelationshipCompletionStats>? relationshipStats = null,
        HighLowScoresResult? highLowScores = null,
        LatentStrengthsBlindspotsResult? latentStrengthsBlindspots = null)
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

        // Replace self-assessment status (computed from ReportAnalyticsRepository)
        html = html.Replace("{{SELF_ASSESSMENT_STATUS}}", selfAssessmentStatus ?? "Not Available");

        // Replace relationship completion rows
        html = html.Replace("{{RELATIONSHIP_COMPLETION_ROWS}}", GenerateRelationshipCompletionRows(relationshipStats));

        // Replace high/low scores rows
        html = html.Replace("{{HIGHEST_SCORES_ROWS}}", GenerateHighLowScoresRows(highLowScores?.HighestScores, isHighest: true));
        html = html.Replace("{{LOWEST_SCORES_ROWS}}", GenerateHighLowScoresRows(highLowScores?.LowestScores, isHighest: false));

        // Replace latent strengths and blindspots rows
        html = html.Replace("{{LATENT_STRENGTHS_ROWS}}", GenerateGapScoreRows(latentStrengthsBlindspots?.LatentStrengths, isLatentStrength: true));
        html = html.Replace("{{BLINDSPOTS_ROWS}}", GenerateGapScoreRows(latentStrengthsBlindspots?.Blindspots, isLatentStrength: false));

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

