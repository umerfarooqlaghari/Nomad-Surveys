using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
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
    private readonly NomadSurveysDbContext? _dbContext;
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

        // Get DbContext for chart images (optional dependency)
        try
        {
            _dbContext = serviceProvider.GetService<NomadSurveysDbContext>();
        }
        catch
        {
            _dbContext = null;
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
            AgreementChartResult? agreementChartData = null;
            RaterGroupSummaryResult? raterGroupSummary = null;
            ClusterCompetencyHierarchyResult? clusterHierarchy = null;
            OpenEndedFeedbackResult? openEndedFeedback = null;
            var effectiveTenantId = tenantId ?? subjectTenantId;

            bool useDynamicGeneration = false;

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

                try
                {
                    agreementChartData = await _reportAnalyticsRepository.GetAgreementChartDataAsync(
                        subjectId.Value, surveyId.Value, effectiveTenantId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get agreement chart data for preview. Using mock data.");
                }

                try
                {
                    raterGroupSummary = await _reportAnalyticsRepository.GetRaterGroupSummaryAsync(
                        subjectId.Value, surveyId.Value, effectiveTenantId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get rater group summary for preview. Using mock data.");
                }

                // Get cluster-competency hierarchy for Part 2 dynamic pages
                try
                {
                    clusterHierarchy = await _reportAnalyticsRepository.GetClusterCompetencyHierarchyAsync(
                        subjectId.Value, surveyId.Value, effectiveTenantId.Value);

                    // Use dynamic generation if we have cluster data
                    useDynamicGeneration = clusterHierarchy?.Clusters?.Count > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get cluster hierarchy for preview. Using mock data.");
                }

                // Get open-ended feedback for dynamic generation
                if (useDynamicGeneration)
                {
                    try
                    {
                        openEndedFeedback = await _reportAnalyticsRepository.GetOpenEndedFeedbackAsync(
                            subjectId.Value, surveyId.Value, effectiveTenantId.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get open-ended feedback for preview.");
                    }
                }
            }

            // Fetch chart images for this survey
            List<ReportChartImage>? chartImages = null;
            if (surveyId.HasValue && effectiveTenantId.HasValue && _dbContext != null)
            {
                try
                {
                    chartImages = await _dbContext.ReportChartImages
                        .Where(i => i.SurveyId == surveyId.Value && i.TenantId == effectiveTenantId.Value)
                        .ToListAsync();
                    _logger.LogInformation("Loaded {Count} chart images for survey {SurveyId}", chartImages.Count, surveyId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load chart images for preview.");
                }
            }

            // Use dynamic report generation when we have cluster hierarchy data
            if (useDynamicGeneration && clusterHierarchy != null)
            {
                var totalCompetencies = clusterHierarchy.Clusters.Sum(c => c.Competencies.Count);
                _logger.LogInformation("Using dynamic report generation with {ClusterCount} clusters and {CompetencyCount} competencies",
                    clusterHierarchy.Clusters.Count, totalCompetencies);

                // Use mock feedback data if no actual feedback exists (for preview purposes)
                var feedbackItems = openEndedFeedback?.Items;
                if (feedbackItems == null || feedbackItems.Count == 0)
                {
                    feedbackItems = GetMockOpenEndedFeedback();
                    _logger.LogInformation("Using mock open-ended feedback data for preview ({Count} items)", feedbackItems.Count);
                }

                var html = await BuildDynamicReportHtmlAsync(
                    companyName,
                    companyLogoUrl,
                    coverImageUrl,
                    primaryColor,
                    secondaryColor,
                    tertiaryColor,
                    clusterHierarchy,
                    feedbackItems,
                    chartImages);

                // Still need to replace subject name and other placeholders in Part 1
                html = html.Replace("{{SUBJECT_NAME}}", EscapeHtml(subjectName ?? "Subject Name"));
                html = html.Replace("{{SELF_ASSESSMENT_STATUS}}", EscapeHtml(selfAssessmentStatus));

                // Replace other Part 1 placeholders
                html = ReplaceAdditionalPlaceholdersInDynamicReport(html, companyName, subjectName,
                    selfAssessmentStatus, relationshipStats, highLowScores, latentStrengthsBlindspots,
                    agreementChartData, raterGroupSummary, clusterHierarchy, chartImages);

                return html;
            }

            // Fallback to static template with mock data
            _logger.LogInformation("Using static template (no subject data or insufficient competency data)");
            var htmlTemplate = await LoadTemplateAsync();
            var staticHtml = ReplacePlaceholdersForPreview(htmlTemplate, companyName, companyLogoUrl, coverImageUrl,
                primaryColor, secondaryColor, tertiaryColor, subjectName, selfAssessmentStatus,
                relationshipStats, highLowScores, latentStrengthsBlindspots, agreementChartData, raterGroupSummary);

            return staticHtml;
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

    /// <summary>
    /// Replaces additional placeholders in the dynamic report (those not handled by BuildDynamicReportHtmlAsync)
    /// </summary>
    private string ReplaceAdditionalPlaceholdersInDynamicReport(
        string html,
        string companyName,
        string? subjectName,
        string selfAssessmentStatus,
        List<RelationshipCompletionStats>? relationshipStats,
        HighLowScoresResult? highLowScores,
        LatentStrengthsBlindspotsResult? latentStrengthsBlindspots,
        AgreementChartResult? agreementChartData,
        RaterGroupSummaryResult? raterGroupSummary,
        ClusterCompetencyHierarchyResult? clusterHierarchy = null,
        List<ReportChartImage>? chartImages = null)
    {
        // Cluster and competency placeholders (Page 3 & 4)
        html = ReplaceClusterCompetencyPlaceholders(html, clusterHierarchy);

        // Page 4 and Page 6 images
        var page4Image = chartImages?.FirstOrDefault(i => i.ImageType == "Page4");
        var page6Image = chartImages?.FirstOrDefault(i => i.ImageType == "Page6");
        html = html.Replace("{{PAGE_4_IMAGE}}", GetPageImageHtml(page4Image?.ImageUrl, "Framework Image"));
        html = html.Replace("{{PAGE_6_IMAGE}}", GetPageImageHtml(page6Image?.ImageUrl, "Radar Chart"));

        // Relationship completion stats (Page 5)
        html = html.Replace("{{RELATIONSHIP_COMPLETION_ROWS}}", GenerateRelationshipCompletionRows(relationshipStats));

        // High/Low Scores (Page 7)
        var hasHighLowData = (highLowScores?.HighestScores?.Count > 0) || (highLowScores?.LowestScores?.Count > 0);
        if (hasHighLowData)
        {
            html = html.Replace("{{HIGHEST_SCORES_ROWS}}", GenerateHighLowScoresRows(highLowScores!.HighestScores, true));
            html = html.Replace("{{LOWEST_SCORES_ROWS}}", GenerateHighLowScoresRows(highLowScores!.LowestScores, false));
        }
        else
        {
            html = html.Replace("{{HIGHEST_SCORES_ROWS}}", GenerateHighLowScoresRows(null, true));
            html = html.Replace("{{LOWEST_SCORES_ROWS}}", GenerateHighLowScoresRows(null, false));
        }

        // Latent Strengths & Blindspots (Page 8)
        var hasLatentData = (latentStrengthsBlindspots?.LatentStrengths?.Count > 0) || (latentStrengthsBlindspots?.Blindspots?.Count > 0);
        if (hasLatentData)
        {
            html = html.Replace("{{LATENT_STRENGTHS_ROWS}}", GenerateGapScoreRows(latentStrengthsBlindspots!.LatentStrengths, true));
            html = html.Replace("{{BLINDSPOTS_ROWS}}", GenerateGapScoreRows(latentStrengthsBlindspots!.Blindspots, false));
        }
        else
        {
            html = html.Replace("{{LATENT_STRENGTHS_ROWS}}", GenerateMockGapScoreRows(true));
            html = html.Replace("{{BLINDSPOTS_ROWS}}", GenerateMockGapScoreRows(false));
        }

        // Agreement Chart (Page 12)
        var hasAgreementData = agreementChartData?.CompetencyItems?.Count > 0;
        if (hasAgreementData)
        {
            html = html.Replace("{{AGREEMENT_CHART_ITEMS}}", GenerateAgreementChartItems(agreementChartData!.CompetencyItems));
        }
        else
        {
            html = html.Replace("{{AGREEMENT_CHART_ITEMS}}", GenerateMockAgreementChartItems());
        }

        // Gap Chart (Page 10)
        var hasRaterGroupSummaryData = raterGroupSummary?.CompetencyItems?.Count > 0;
        if (hasRaterGroupSummaryData)
        {
            html = html.Replace("{{GAP_CHART_ITEMS}}", GenerateGapChartItems(raterGroupSummary!.CompetencyItems));
            html = html.Replace("{{RATER_GROUP_SUMMARY_TABLE}}", GenerateRaterGroupSummaryTable(raterGroupSummary));
        }
        else
        {
            html = html.Replace("{{GAP_CHART_ITEMS}}", GenerateMockGapChartItems());
            html = html.Replace("{{RATER_GROUP_SUMMARY_TABLE}}", GenerateMockRaterGroupSummaryTable());
        }

        return html;
    }

    /// <summary>
    /// Replaces cluster and competency placeholders with real or mock data
    /// </summary>
    private string ReplaceClusterCompetencyPlaceholders(string html, ClusterCompetencyHierarchyResult? clusterHierarchy)
    {
        if (clusterHierarchy?.Clusters?.Count > 0)
        {
            var clusters = clusterHierarchy.Clusters;
            var clusterCount = clusters.Count;
            var totalDimensions = clusters.Sum(c => c.Competencies.Count);
            var dimensionsPerCluster = clusterCount > 0 ? totalDimensions / clusterCount : 0;

            // Format cluster names list (e.g., "Leading Solutions, Impacting People and Driving Business")
            var clusterNames = clusters.Select(c => c.ClusterName).ToList();
            var clusterNamesList = FormatListWithAnd(clusterNames);

            // Get example dimension names (first 2 competencies from first cluster)
            var exampleDimensions = clusters
                .SelectMany(c => c.Competencies)
                .Take(2)
                .Select(c => c.CompetencyName)
                .ToList();
            var exampleDimensionsList = exampleDimensions.Count > 0 
                ? string.Join(", ", exampleDimensions) + " etc"
                : "various dimensions";

            // Convert number to word for cluster count
            var clusterCountWord = NumberToWord(clusterCount);

            html = html.Replace("{{CLUSTER_NAMES_LIST}}", EscapeHtml(clusterNamesList));
            html = html.Replace("{{CLUSTER_COUNT}}", clusterCount.ToString());
            html = html.Replace("{{CLUSTER_COUNT_WORD}}", clusterCountWord);
            html = html.Replace("{{TOTAL_DIMENSIONS_COUNT}}", totalDimensions.ToString());
            html = html.Replace("{{DIMENSIONS_PER_CLUSTER}}", dimensionsPerCluster.ToString());
            html = html.Replace("{{EXAMPLE_DIMENSIONS}}", EscapeHtml(exampleDimensionsList));
        }
        else
        {
            // Mock data for preview when no cluster hierarchy is available
            html = html.Replace("{{CLUSTER_NAMES_LIST}}", "Leading Solutions, Impacting People and Driving Business");
            html = html.Replace("{{CLUSTER_COUNT}}", "3");
            html = html.Replace("{{CLUSTER_COUNT_WORD}}", "three");
            html = html.Replace("{{TOTAL_DIMENSIONS_COUNT}}", "12");
            html = html.Replace("{{DIMENSIONS_PER_CLUSTER}}", "4");
            html = html.Replace("{{EXAMPLE_DIMENSIONS}}", "Inventive Execution, Purposeful Conversations etc");
        }

        return html;
    }

    /// <summary>
    /// Formats a list with "and" before the last item (e.g., "A, B and C")
    /// </summary>
    private string FormatListWithAnd(List<string> items)
    {
        if (items == null || items.Count == 0) return "";
        if (items.Count == 1) return items[0];
        if (items.Count == 2) return $"{items[0]} and {items[1]}";
        
        var allButLast = string.Join(", ", items.Take(items.Count - 1));
        return $"{allButLast} and {items.Last()}";
    }

    /// <summary>
    /// Converts a number to its word representation (for small numbers)
    /// </summary>
    private string NumberToWord(int number)
    {
        return number switch
        {
            1 => "one",
            2 => "two",
            3 => "three",
            4 => "four",
            5 => "five",
            6 => "six",
            7 => "seven",
            8 => "eight",
            9 => "nine",
            10 => "ten",
            _ => number.ToString()
        };
    }

    public async Task<string> GenerateReportHtmlAsync(
        Guid subjectId,
        Guid? surveyId,
        Guid tenantId,
        string companyName,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null)
    {
        try
        {

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
            AgreementChartResult? agreementChartData = null;
            RaterGroupSummaryResult? raterGroupSummary = null;
            ClusterCompetencyHierarchyResult? clusterHierarchy = null;
            OpenEndedFeedbackResult? openEndedFeedback = null;

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

                try
                {
                    agreementChartData = await _reportAnalyticsRepository.GetAgreementChartDataAsync(
                        subjectId, surveyId.Value, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get agreement chart data for subject {SubjectId}", subjectId);
                }

                try
                {
                    raterGroupSummary = await _reportAnalyticsRepository.GetRaterGroupSummaryAsync(
                        subjectId, surveyId.Value, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get rater group summary for subject {SubjectId}", subjectId);
                }

                try
                {
                    clusterHierarchy = await _reportAnalyticsRepository.GetClusterCompetencyHierarchyAsync(
                        subjectId, surveyId.Value, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get cluster hierarchy for subject {SubjectId}", subjectId);
                }

                if (clusterHierarchy?.Clusters?.Count > 0)
                {
                    try
                    {
                        openEndedFeedback = await _reportAnalyticsRepository.GetOpenEndedFeedbackAsync(
                            subjectId, surveyId.Value, tenantId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get open-ended feedback for subject {SubjectId}", subjectId);
                    }
                }
            }

            // Fetch chart images for this survey
            List<ReportChartImage>? chartImages = null;
            if (surveyId.HasValue && _dbContext != null)
            {
                try
                {
                    chartImages = await _dbContext.ReportChartImages
                        .Where(i => i.SurveyId == surveyId.Value && i.TenantId == tenantId)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load chart images for subject {SubjectId}", subjectId);
                }
            }

            string html;
            if (clusterHierarchy?.Clusters?.Count > 0)
            {
                // Use dynamic generation if we have cluster data
                html = await BuildDynamicReportHtmlAsync(
                    companyName,
                    companyLogoUrl,
                    coverImageUrl,
                    primaryColor,
                    secondaryColor,
                    tertiaryColor,
                    clusterHierarchy,
                    openEndedFeedback?.Items,
                    chartImages);

                // Replace Part 1 and brand placeholders
                html = html.Replace("{{SUBJECT_NAME}}", EscapeHtml(reportData.SubjectName ?? "N/A"));
                html = html.Replace("{{SELF_ASSESSMENT_STATUS}}", EscapeHtml(selfAssessmentStatus));

                html = ReplaceAdditionalPlaceholdersInDynamicReport(html, companyName, reportData.SubjectName,
                    selfAssessmentStatus, relationshipStats, highLowScores, latentStrengthsBlindspots,
                    agreementChartData, raterGroupSummary, clusterHierarchy, chartImages);
            }
            else
            {
                // Fallback to static template
                var htmlTemplate = await LoadTemplateAsync();
                html = ReplacePlaceholders(htmlTemplate, reportData, companyName, companyLogoUrl, coverImageUrl, 
                    primaryColor, secondaryColor, tertiaryColor, selfAssessmentStatus, 
                    relationshipStats, highLowScores, latentStrengthsBlindspots);
            }

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

    public async Task<PdfGenerationResult> GenerateReportPdfAsync(
        Guid subjectId,
        Guid? surveyId,
        Guid tenantId,
        string companyName,
        string? companyLogoUrl = null,
        string? coverImageUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? tertiaryColor = null)
    {
        try
        {
            // Fetch subject name for the filename
            string subjectName = "Report";
            if (_subjectService != null)
            {
                var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
                if (subject?.Employee != null)
                {
                    subjectName = subject.Employee.FullName ?? "Report";
                }
            }
            
            // Fallback: Try to get subject name from reporting service if not found yet
            if (subjectName == "Report")
            {
                try 
                {
                    var subjectReport = await _reportingService.GetSubjectReportAsync(subjectId, surveyId, tenantId);
                    if (!string.IsNullOrEmpty(subjectReport?.SubjectName))
                    {
                        subjectName = subjectReport.SubjectName;
                    }
                }
                catch
                {
                    // Ignore error, keep using default
                }
            }

            // Generate HTML first
            var html = await GenerateReportHtmlAsync(
                subjectId, surveyId, tenantId, companyName, companyLogoUrl, coverImageUrl, primaryColor, secondaryColor, tertiaryColor);

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

            // Sanitize subject name for filename (remove invalid characters)
            var sanitizedName = SanitizeFileName(subjectName);

            return new PdfGenerationResult
            {
                PdfBytes = pdfBytes,
                SubjectName = subjectName,
                FileName = $"{sanitizedName}.pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report PDF for subject {SubjectId}", subjectId);
            throw;
        }
    }

    /// <summary>
    /// Sanitizes a string to be used as a valid filename
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Replace spaces with underscores for cleaner filenames
        sanitized = sanitized.Replace(" ", "_");

        // Ensure the filename is not empty
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Report";
        }

        return sanitized;
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
        LatentStrengthsBlindspotsResult? latentStrengthsBlindspots = null,
        AgreementChartResult? agreementChartData = null,
        RaterGroupSummaryResult? raterGroupSummary = null)
    {
        var html = template;

        // Replace brand colors
        html = html.Replace("{{PRIMARY_COLOR}}", primaryColor ?? "#0455A4");
        html = html.Replace("{{SECONDARY_COLOR}}", secondaryColor ?? "#1D8F6C");
        html = html.Replace("{{TERTIARY_COLOR}}", tertiaryColor ?? "#6C757D");

        // Replace company information
        html = html.Replace("{{COMPANY_NAME}}", companyName ?? "Company Name");

        // Replace cluster and competency placeholders (with mock data for preview fallback)
        html = ReplaceClusterCompetencyPlaceholders(html, null);

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

        // Replace agreement chart - use real data if available, otherwise mock data
        var hasAgreementChartData = agreementChartData?.CompetencyItems?.Count > 0;
        if (hasAgreementChartData)
        {
            html = html.Replace("{{AGREEMENT_CHART_ITEMS}}", GenerateAgreementChartItems(agreementChartData!.CompetencyItems));
        }
        else
        {
            html = html.Replace("{{AGREEMENT_CHART_ITEMS}}", GenerateMockAgreementChartItems());
        }

        // Replace rater group summary table - use real data if available, otherwise mock data
        var hasRaterGroupSummaryData = raterGroupSummary?.CompetencyItems?.Count > 0;
        if (hasRaterGroupSummaryData)
        {
            html = html.Replace("{{RATER_GROUP_SUMMARY_TABLE}}", GenerateRaterGroupSummaryTable(raterGroupSummary!));
        }
        else
        {
            html = html.Replace("{{RATER_GROUP_SUMMARY_TABLE}}", GenerateMockRaterGroupSummaryTable());
        }

        // Replace gap chart - reuse rater group summary data (Self vs Others per competency)
        if (hasRaterGroupSummaryData)
        {
            html = html.Replace("{{GAP_CHART_ITEMS}}", GenerateGapChartItems(raterGroupSummary!.CompetencyItems));
        }
        else
        {
            html = html.Replace("{{GAP_CHART_ITEMS}}", GenerateMockGapChartItems());
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

        // Replace radial chart images for pages 16-30
        for (int page = 16; page <= 30; page++)
        {
            var placeholder = $"{{{{RADIAL_CHART_IMAGE_{page}}}}}";
            // Default SVG radial chart placeholder
            var defaultRadialChart = @"
                <svg viewBox=""0 0 500 500"" class=""radial-svg"">
                    <defs>
                        <path id=""curveLeading"" d=""M 250, 250 m -200, 0 a 200,200 0 0,1 200,-200"" />
                        <path id=""curveImpacting"" d=""M 250, 250 m 0, -200 a 200,200 0 0,1 200,200"" />
                        <path id=""curveDriving"" d=""M 250, 250 m 200, 0 a 200,200 0 1,1 -400,0"" />
                    </defs>
                    <path d=""M 250, 250 L 50, 250 A 200,200 0 0,1 250,50 Z"" fill=""#FFC000"" stroke=""white"" stroke-width=""2"" />
                    <path d=""M 250, 250 L 250, 50 A 200,200 0 0,1 450,250 Z"" fill=""#00B0F0"" stroke=""white"" stroke-width=""2"" />
                    <path d=""M 250, 250 L 450, 250 A 200,200 0 0,1 50,250 Z"" fill=""#364152"" stroke=""white"" stroke-width=""2"" />
                    <circle cx=""250"" cy=""250"" r=""150"" fill=""white"" />
                    <text font-size=""24"" font-weight=""bold"" fill=""white"" letter-spacing=""2"">
                        <textPath href=""#curveLeading"" startOffset=""10%"" text-anchor=""middle"">LEADING SOLUTIONS</textPath>
                    </text>
                    <text font-size=""24"" font-weight=""bold"" fill=""white"" letter-spacing=""2"">
                        <textPath href=""#curveImpacting"" startOffset=""25%"" text-anchor=""middle"">IMPACTING PEOPLE</textPath>
                    </text>
                    <text font-size=""24"" font-weight=""bold"" fill=""white"" letter-spacing=""2"">
                        <textPath href=""#curveDriving"" startOffset=""50%"" text-anchor=""middle"">DRIVING BUSINESS</textPath>
                    </text>
                </svg>";
            html = html.Replace(placeholder, defaultRadialChart);
        }

        // Replace questions table with mock data
        html = html.Replace("{{QUESTIONS_TABLE}}", GenerateMockQuestionsTable());

        // Replace Ascend logo with base64 for visibility
        var ascendLogoBase64 = GetAscendLogoBase64();
        if (!string.IsNullOrEmpty(ascendLogoBase64))
        {
            html = html.Replace("/logos/ascendevelopment_logo.jpeg", ascendLogoBase64);
        }

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
        foreach (var item in items.Take(4))
        {
            sb.AppendLine($@"
        <tr>
            <td>{item.Rank}</td>
            <td>{System.Web.HttpUtility.HtmlEncode(item.Dimension)}</td>
            <td>{System.Web.HttpUtility.HtmlEncode(CleanQuestionText(item.Item))}</td>
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
        foreach (var item in items.Take(4))
        {
            // Format gap with sign (+ for positive, - for negative)
            var gapDisplay = item.Gap >= 0 ? $"+{item.Gap:0.00}" : $"{item.Gap:0.00}";
            // Color code: green for positive gap, red for negative gap
            var gapColor = item.Gap >= 0 ? "#1D8F6C" : "#DC3545";

            sb.AppendLine($@"
        <tr>
            <td>{item.Rank}</td>
            <td>{System.Web.HttpUtility.HtmlEncode(item.ScoringCategory)}</td>
            <td>{System.Web.HttpUtility.HtmlEncode(CleanQuestionText(item.Item))}</td>
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

    /// <summary>
    /// Generates HTML for agreement chart items from real data
    /// </summary>
    private string GenerateAgreementChartItems(List<AgreementChartItem> items)
    {
        var sb = new StringBuilder();

        foreach (var item in items)
        {
            sb.AppendLine($@"
                <div class=""agreement-chart-item"">
                    <div class=""item-label"">{EscapeHtml(item.CompetencyName)}</div>
                    <div class=""bubble-area"">");

            // ====== START: Updated bubble positioning for 0-4 scale ======
            // Generate bubbles for each score level (0-4) that has responses
            foreach (var scoreEntry in item.ScoreDistribution.Where(s => s.Value > 0))
            {
                var score = scoreEntry.Key;
                var count = scoreEntry.Value;

                // Calculate X position on 0-4 scale (0% = score 0, 100% = score 4)
                // Score 0 = 0%, Score 1 = 25%, Score 2 = 50%, Score 3 = 75%, Score 4 = 100%
                var xPos = score / 4.0 * 100;

                // Determine bubble size class based on count (1-5+)
                var sizeClass = count >= 5 ? "size-5" : $"size-{count}";

                sb.AppendLine($@"                        <div class=""bubble {sizeClass}"" style=""--x-pos: {xPos:F1}%;""></div>");
            }
            // ====== END: Updated bubble positioning for 0-4 scale ======

            sb.AppendLine(@"                    </div>
                </div>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates mock HTML for agreement chart items for preview
    /// </summary>
    private string GenerateMockAgreementChartItems()
    {
        var mockData = new List<AgreementChartItem>
        {
            new() { CompetencyName = "Inventive Execution", ScoreDistribution = new Dictionary<int, int> { {1, 1}, {3, 2}, {4, 5}, {5, 3} } },
            new() { CompetencyName = "Expansive Thinking", ScoreDistribution = new Dictionary<int, int> { {2, 1}, {3, 2}, {4, 4}, {5, 1} } },
            new() { CompetencyName = "Decision Making", ScoreDistribution = new Dictionary<int, int> { {3, 5}, {4, 2}, {5, 1} } },
            new() { CompetencyName = "Dynamic Sensing", ScoreDistribution = new Dictionary<int, int> { {2, 1}, {3, 2}, {4, 1}, {5, 3} } },
            new() { CompetencyName = "Nurture Growth", ScoreDistribution = new Dictionary<int, int> { {1, 1}, {3, 3}, {4, 5} } },
            new() { CompetencyName = "Purposeful Conversations", ScoreDistribution = new Dictionary<int, int> { {2, 1}, {3, 5}, {5, 2} } },
            new() { CompetencyName = "Network and Influence", ScoreDistribution = new Dictionary<int, int> { {2, 1}, {4, 4}, {5, 3} } },
            new() { CompetencyName = "People Agility", ScoreDistribution = new Dictionary<int, int> { {1, 1}, {3, 2}, {4, 5}, {5, 1} } },
            new() { CompetencyName = "Unprecedented Excellence", ScoreDistribution = new Dictionary<int, int> { {2, 3}, {4, 2}, {5, 1} } },
            new() { CompetencyName = "Catalytic Learning", ScoreDistribution = new Dictionary<int, int> { {1, 1}, {2, 2}, {3, 5}, {5, 1} } },
            new() { CompetencyName = "Enterprising Drive", ScoreDistribution = new Dictionary<int, int> { {1, 1}, {2, 3}, {3, 5}, {5, 1} } },
            new() { CompetencyName = "Organizational Relatability", ScoreDistribution = new Dictionary<int, int> { {2, 1}, {4, 5}, {5, 2} } },
        };

        return GenerateAgreementChartItems(mockData);
    }

    /// <summary>
    /// Generates HTML for rater group summary table from real data
    /// </summary>
    private string GenerateRaterGroupSummaryTable(RaterGroupSummaryResult data)
    {
        var sb = new StringBuilder();

        sb.AppendLine(@"<table class=""rater-group-table"">
                <thead>
                    <tr>
                        <th class=""dimension-header"">Dimensions</th>
                        <th class=""score-header self-header"">Self</th>
                        <th class=""score-header others-header"">Others</th>");

        // Add dynamic relationship columns
        foreach (var relationship in data.RelationshipTypes)
        {
            sb.AppendLine($@"                        <th class=""score-header"">{EscapeHtml(relationship)}</th>");
        }

        sb.AppendLine(@"                    </tr>
                </thead>
                <tbody>");

        // Add rows for each competency
        foreach (var item in data.CompetencyItems)
        {
            sb.AppendLine($@"                    <tr>
                        <td class=""dimension-name"">{EscapeHtml(item.CompetencyName)}</td>
                        <td class=""score self-score"">{FormatRaterGroupScore(item.SelfScore)}</td>
                        <td class=""score others-score"">{FormatRaterGroupScore(item.OthersScore)}</td>");

            // Add scores for each relationship type
            foreach (var relationship in data.RelationshipTypes)
            {
                var score = item.RelationshipScores.TryGetValue(relationship, out var s) ? s : null;
                sb.AppendLine($@"                        <td class=""score"">{FormatRaterGroupScore(score)}</td>");
            }

            sb.AppendLine(@"                    </tr>");
        }

        sb.AppendLine(@"                </tbody>
            </table>");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a score for display with 2 decimal places, showing "-" for null values
    /// </summary>
    private static string FormatRaterGroupScore(double? score)
    {
        return score.HasValue ? score.Value.ToString("0.00") : "-";
    }

    /// <summary>
    /// Generates mock HTML for rater group summary table for preview
    /// </summary>
    private string GenerateMockRaterGroupSummaryTable()
    {
        var mockData = new RaterGroupSummaryResult
        {
            RelationshipTypes = new List<string> { "Line Manager", "Peers", "Direct Reports", "Stakeholders" },
            CompetencyItems = new List<RaterGroupSummaryItem>
            {
                new() { CompetencyName = "Inventive Execution", SelfScore = 4.00, OthersScore = 3.22, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 2.60}, {"Peers", 1.58}, {"Direct Reports", 2.00}, {"Stakeholders", 3.52} } },
                new() { CompetencyName = "Expansive Thinking", SelfScore = 3.40, OthersScore = 3.05, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 2.20}, {"Peers", 3.25}, {"Direct Reports", 2.20}, {"Stakeholders", 3.42} } },
                new() { CompetencyName = "Decision Making", SelfScore = 3.60, OthersScore = 3.27, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 3.00}, {"Peers", 3.33}, {"Direct Reports", 3.00}, {"Stakeholders", 3.38} } },
                new() { CompetencyName = "Dynamic Sensing", SelfScore = 4.00, OthersScore = 3.27, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 3.50}, {"Peers", 3.36}, {"Direct Reports", 2.88}, {"Stakeholders", 3.48} } },
                new() { CompetencyName = "Nurture Growth", SelfScore = 3.29, OthersScore = 2.96, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 2.57}, {"Peers", 3.47}, {"Direct Reports", 1.02}, {"Stakeholders", 3.15} } },
                new() { CompetencyName = "Purposeful Conversations", SelfScore = 3.76, OthersScore = 3.23, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 3.00}, {"Peers", 3.57}, {"Direct Reports", 2.63}, {"Stakeholders", 3.26} } },
                new() { CompetencyName = "Network and Influence", SelfScore = 3.67, OthersScore = 3.28, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 2.67}, {"Peers", 3.61}, {"Direct Reports", 2.00}, {"Stakeholders", 3.56} } },
                new() { CompetencyName = "People Agility", SelfScore = 3.40, OthersScore = 2.92, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 2.60}, {"Peers", 3.20}, {"Direct Reports", 1.80}, {"Stakeholders", 3.21} } },
                new() { CompetencyName = "Unprecedented Excellence", SelfScore = 3.67, OthersScore = 3.23, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 3.00}, {"Peers", 3.50}, {"Direct Reports", 2.50}, {"Stakeholders", 3.38} } },
                new() { CompetencyName = "Catalytic Learning", SelfScore = 3.26, OthersScore = 2.77, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 2.00}, {"Peers", 3.00}, {"Direct Reports", 1.50}, {"Stakeholders", 3.23} } },
                new() { CompetencyName = "Enterprising Drive", SelfScore = 3.76, OthersScore = 3.23, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 3.26}, {"Peers", 3.42}, {"Direct Reports", 3.00}, {"Stakeholders", 3.20} } },
                new() { CompetencyName = "Organizational Relatability", SelfScore = 3.50, OthersScore = 3.02, RelationshipScores = new Dictionary<string, double?> { {"Line Manager", 2.26}, {"Peers", 3.16}, {"Direct Reports", 2.38}, {"Stakeholders", 3.30} } },
            }
        };

        return GenerateRaterGroupSummaryTable(mockData);
    }

    /// <summary>
    /// Generates mock open-ended feedback data for preview
    /// </summary>
    private List<OpenEndedFeedbackItem> GetMockOpenEndedFeedback()
    {
        return new List<OpenEndedFeedbackItem>
        {
            // Stop Doing question
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Managing individual's deliverables", RaterType = "Manager" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Stop spending time alone in my office.", RaterType = "Direct" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Stop saying yes to every requirement that comes to me.", RaterType = "Peer" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Stop being a soft and accepting person all the time.", RaterType = "Peer" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Stop spending too much time at/for work and should create a suitable work-life balance.", RaterType = "Direct" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Should avoid himself in to involve in minor activities and let the team do.", RaterType = "Stakeholder" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Stop doing many tasks by himself.", RaterType = "Peer" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Stopping his team from exploring innovative ideas. Stop micro managing the team. Stop viewing things from internal perspective. Be more flexible.", RaterType = "SkipLine" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Being too much detail oriented which at times leads to losing the focus.", RaterType = "Peer" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Micromanaging and delaying decision making.", RaterType = "Manager" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "He should be to the point, crisp and result oriented during his communication. Too much details also make things complex.", RaterType = "Stakeholder" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "N/A", RaterType = "Direct" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Stop getting worried from team members who take you for granted.", RaterType = "Peer" },
            new() { QuestionText = "What should this person stop doing in order to become more effective as a leader?", ResponseText = "Be concise and crisp in communication.", RaterType = "Stakeholder" },

            // Start Doing question
            new() { QuestionText = "What should this person start doing in order to become more effective as a leader?", ResponseText = "He should start delegating tasks to other members and not take all responsibilities on his own.", RaterType = "Manager" },
            new() { QuestionText = "What should this person start doing in order to become more effective as a leader?", ResponseText = "Start visiting other teams as well. Understand challenges at grass root level.", RaterType = "Direct" },
            new() { QuestionText = "What should this person start doing in order to become more effective as a leader?", ResponseText = "Start saying no to things that are not priority or not urgent.", RaterType = "Peer" },
            new() { QuestionText = "What should this person start doing in order to become more effective as a leader?", ResponseText = "Start being more assertive when needed.", RaterType = "Peer" },
            new() { QuestionText = "What should this person start doing in order to become more effective as a leader?", ResponseText = "Start focusing on strategic initiatives rather than day-to-day operations.", RaterType = "Stakeholder" },
            new() { QuestionText = "What should this person start doing in order to become more effective as a leader?", ResponseText = "Start having regular one-on-one meetings with team members.", RaterType = "Direct" },

            // Continue Doing question
            new() { QuestionText = "What should this person continue doing to remain effective as a leader?", ResponseText = "Continue to be approachable and supportive of the team.", RaterType = "Manager" },
            new() { QuestionText = "What should this person continue doing to remain effective as a leader?", ResponseText = "Continue providing clear direction and guidance to the team.", RaterType = "Direct" },
            new() { QuestionText = "What should this person continue doing to remain effective as a leader?", ResponseText = "Continue to maintain open communication with all stakeholders.", RaterType = "Peer" },
            new() { QuestionText = "What should this person continue doing to remain effective as a leader?", ResponseText = "Continue being a good listener and taking feedback constructively.", RaterType = "Stakeholder" },
            new() { QuestionText = "What should this person continue doing to remain effective as a leader?", ResponseText = "Continue to lead by example and demonstrate high ethical standards.", RaterType = "SkipLine" },
        };
    }

    /// <summary>
    /// Generates HTML for gap chart items from real data
    /// </summary>
    private string GenerateGapChartItems(List<RaterGroupSummaryItem> items)
    {
        var sb = new StringBuilder();

        // Limit to 15 competencies
        var limitedItems = items.Take(15).ToList();

        // ====== START: Updated gap chart generation for 0-4 scale with gap value inside chart ======
        foreach (var item in limitedItems)
        {
            // Skip if either score is missing
            if (!item.SelfScore.HasValue || !item.OthersScore.HasValue)
                continue;

            var selfScore = item.SelfScore.Value;
            var othersScore = item.OthersScore.Value;

            // Gap = Others - Self
            // Positive(+) = others rated you higher than you rated yourself
            // Negative(-) = others rated you lower than you rated yourself
            var gap = othersScore - selfScore;

            // Calculate positions on 0-4 scale (0% = score 0, 100% = score 4)
            var selfPos = selfScore / 4 * 100;
            var othersPos = othersScore / 4 * 100;

            // Determine line start and end (always from lower to higher position)
            var lineStart = Math.Min(selfPos, othersPos);
            var lineEnd = Math.Max(selfPos, othersPos);

            // Format gap value with sign (explicit minus sign for negative values)
            var gapValue = gap >= 0 ? $"+{gap:F2}" : $"−{Math.Abs(gap):F2}";

            // Position gap value:
            // - Negative gap: place behind "others" bubble (others rated lower)
            // - Positive gap: place behind "self" bubble (self rated lower)
            var gapValuePos = gap < 0 ? othersPos : selfPos;

            sb.AppendLine($@"                <div class=""gap-chart-item"">
                    <div class=""item-label"">{EscapeHtml(item.CompetencyName)}</div>
                    <div class=""chart-bar-area"">
                        <span class=""gap-value"" style=""--gap-value-pos: {gapValuePos:F1}%;"">{gapValue}</span>
                        <div class=""bar-line"" style=""--line-start: {lineStart:F1}%; --line-end: {lineEnd:F1}%;""></div>
                        <div class=""point self-point"" style=""--point-pos: {selfPos:F1}%;""></div>
                        <div class=""point others-point"" style=""--point-pos: {othersPos:F1}%;""></div>
                    </div>
                </div>");
        }
        // ====== END: Updated gap chart generation for 0-4 scale with gap value inside chart ======

        return sb.ToString();
    }

    /// <summary>
    /// Generates mock HTML for gap chart items for preview
    /// </summary>
    private string GenerateMockGapChartItems()
    {
        var mockItems = new List<RaterGroupSummaryItem>
        {
            new() { CompetencyName = "Inventive Execution", SelfScore = 4.00, OthersScore = 3.22 },
            new() { CompetencyName = "Expansive Thinking", SelfScore = 3.40, OthersScore = 3.05 },
            new() { CompetencyName = "Decision Making", SelfScore = 3.60, OthersScore = 3.27 },
            new() { CompetencyName = "Dynamic Sensing", SelfScore = 4.00, OthersScore = 3.27 },
            new() { CompetencyName = "Nurture Growth", SelfScore = 3.29, OthersScore = 2.96 },
            new() { CompetencyName = "Purposeful Conversations", SelfScore = 3.76, OthersScore = 3.23 },
            new() { CompetencyName = "Network and Influence", SelfScore = 3.67, OthersScore = 3.28 },
            new() { CompetencyName = "People Agility", SelfScore = 3.40, OthersScore = 2.92 },
            new() { CompetencyName = "Unprecedented Excellence", SelfScore = 3.67, OthersScore = 3.23 },
            new() { CompetencyName = "Catalytic Learning", SelfScore = 3.26, OthersScore = 2.77 },
            new() { CompetencyName = "Enterprising Drive", SelfScore = 3.76, OthersScore = 3.23 },
            new() { CompetencyName = "Organizational Relatability", SelfScore = 3.50, OthersScore = 3.02 },
        };

        return GenerateGapChartItems(mockItems);
    }

    private string ReplacePlaceholders(
        string template,
        ComprehensiveReportResponse reportData,
        string companyName,
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

        // Replace company information
        html = html.Replace("{{COMPANY_NAME}}", EscapeHtml(companyName));

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

        // Replace radial chart images for pages 16-30
        for (int page = 16; page <= 30; page++)
        {
            var placeholder = $"{{{{RADIAL_CHART_IMAGE_{page}}}}}";
            // Default SVG radial chart placeholder
            var defaultRadialChart = @"
                <svg viewBox=""0 0 500 500"" class=""radial-svg"">
                    <defs>
                        <path id=""curveLeading"" d=""M 250, 250 m -200, 0 a 200,200 0 0,1 200,-200"" />
                        <path id=""curveImpacting"" d=""M 250, 250 m 0, -200 a 200,200 0 0,1 200,200"" />
                        <path id=""curveDriving"" d=""M 250, 250 m 200, 0 a 200,200 0 1,1 -400,0"" />
                    </defs>
                    <path d=""M 250, 250 L 50, 250 A 200,200 0 0,1 250,50 Z"" fill=""#FFC000"" stroke=""white"" stroke-width=""2"" />
                    <path d=""M 250, 250 L 250, 50 A 200,200 0 0,1 450,250 Z"" fill=""#00B0F0"" stroke=""white"" stroke-width=""2"" />
                    <path d=""M 250, 250 L 450, 250 A 200,200 0 0,1 50,250 Z"" fill=""#364152"" stroke=""white"" stroke-width=""2"" />
                    <circle cx=""250"" cy=""250"" r=""150"" fill=""white"" />
                    <text font-size=""24"" font-weight=""bold"" fill=""white"" letter-spacing=""2"">
                        <textPath href=""#curveLeading"" startOffset=""10%"" text-anchor=""middle"">LEADING SOLUTIONS</textPath>
                    </text>
                    <text font-size=""24"" font-weight=""bold"" fill=""white"" letter-spacing=""2"">
                        <textPath href=""#curveImpacting"" startOffset=""25%"" text-anchor=""middle"">IMPACTING PEOPLE</textPath>
                    </text>
                    <text font-size=""24"" font-weight=""bold"" fill=""white"" letter-spacing=""2"">
                        <textPath href=""#curveDriving"" startOffset=""50%"" text-anchor=""middle"">DRIVING BUSINESS</textPath>
                    </text>
                </svg>";
            html = html.Replace(placeholder, defaultRadialChart);
        }

        // Replace questions table
        html = html.Replace("{{QUESTIONS_TABLE}}", GenerateQuestionsTable(reportData));

        // Replace Ascend logo with base64 for PDF visibility
        var ascendLogoBase64 = GetAscendLogoBase64();
        if (!string.IsNullOrEmpty(ascendLogoBase64))
        {
            html = html.Replace("/logos/ascendevelopment_logo.jpeg", ascendLogoBase64);
        }

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
            sb.AppendLine($"<td>{EscapeHtml(CleanQuestionText(question.QuestionText ?? "N/A"))}</td>");
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

// Set page content
await page.SetContentAsync(html);

var pdfBytes = await page.PdfDataAsync(new PdfOptions
{
    Width = "210mm",
    Height = "297mm",
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
                            column.Item().Text("PDF generation encountered an error. Detailed error:")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                            column.Item().PaddingTop(2);
                            column.Item().Text(ex.Message)
                                .FontSize(8)
                                .FontColor(Colors.Red.Medium);
                            column.Item().PaddingTop(5);
                            column.Item().Text(ex.StackTrace)
                                .FontSize(6)
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

    #region Dynamic Report Assembly (Sandwich Method)

    /// <summary>
    /// Builds a comprehensive report using the sandwich method:
    /// Part 1 (Intro Pages 1-15) + Dynamic Cluster/Competency Pages + Dynamic Feedback + Last Page
    /// </summary>
    public async Task<string> BuildDynamicReportHtmlAsync(
        string companyName,
        string? companyLogoUrl,
        string? coverImageUrl,
        string? primaryColor,
        string? secondaryColor,
        string? tertiaryColor,
        ClusterCompetencyHierarchyResult clusterHierarchy,
        List<OpenEndedFeedbackItem>? openEndedFeedback = null,
        List<ReportChartImage>? chartImages = null)
    {
        var sb = new StringBuilder();
        var templatesDir = Path.GetDirectoryName(_templatePath) ?? "";

        // Load the main template to extract sections
        var mainTemplate = await LoadTemplateAsync();

        // STEP 1: Extract Part 1 (DOCTYPE through Page 15)
        var part1Html = ExtractPart1(mainTemplate);
        part1Html = ReplaceBrandPlaceholders(part1Html, companyName, companyLogoUrl, coverImageUrl,
            primaryColor, secondaryColor, tertiaryColor);
        sb.Append(part1Html);

        // STEP 2: Generate dynamic cluster and competency pages
        // For each cluster: generate cluster page, then competency pages for that cluster
        var pageTemplate = await LoadPartTemplateAsync(templatesDir, "Part2_Competency.html");
        foreach (var cluster in clusterHierarchy.Clusters)
        {
            // Find chart image for this cluster
            var clusterChartImage = chartImages?.FirstOrDefault(i =>
                i.ImageType == "Cluster" && i.ClusterName == cluster.ClusterName);

            // Generate cluster page (shows competencies in table)
            var clusterPageHtml = GenerateClusterPage(pageTemplate, cluster,
                clusterHierarchy.RelationshipTypes, companyLogoUrl, clusterChartImage?.ImageUrl);
            sb.Append(clusterPageHtml);

            // Generate competency pages for this cluster (shows questions/items in table)
            foreach (var competency in cluster.Competencies)
            {
                // Find chart image for this competency
                var competencyChartImage = chartImages?.FirstOrDefault(i =>
                    i.ImageType == "Competency" &&
                    i.ClusterName == cluster.ClusterName &&
                    i.CompetencyName == competency.CompetencyName);

                var competencyPageHtml = GenerateCompetencyDetailPage(pageTemplate, competency,
                    clusterHierarchy.RelationshipTypes, companyLogoUrl, competencyChartImage?.ImageUrl);
                sb.Append(competencyPageHtml);
            }
        }

        // STEP 3: Generate open-ended feedback section
        if (openEndedFeedback != null && openEndedFeedback.Count > 0)
        {
            var feedbackTemplate = await LoadPartTemplateAsync(templatesDir, "Part3_Feedback.html");
            var feedbackHtml = GenerateFeedbackSection(feedbackTemplate, openEndedFeedback, companyLogoUrl);
            sb.Append(feedbackHtml);
        }

        // STEP 4: Append last page
        var lastPageTemplate = await LoadPartTemplateAsync(templatesDir, "Part4_LastPage.html");
        var lastPageHtml = lastPageTemplate.Replace("{{COMPANY_LOGO}}", GetLogoHtml(companyLogoUrl));
        sb.Append(lastPageHtml);

        return ReplaceBrandPlaceholders(sb.ToString(), companyName, companyLogoUrl, coverImageUrl, 
            primaryColor, secondaryColor, tertiaryColor);
    }

    /// <summary>
    /// Extracts Part 1 (Pages 1-15) from the main template - everything up to but not including Page 16
    /// </summary>
    private string ExtractPart1(string mainTemplate)
    {
        // Find the marker for Page 16
        var page16Marker = "<!-- Page 16 -->";
        var page16Index = mainTemplate.IndexOf(page16Marker, StringComparison.OrdinalIgnoreCase);

        if (page16Index == -1)
        {
            _logger.LogWarning("Could not find Page 16 marker, returning full template");
            return mainTemplate;
        }

        // Extract everything before Page 16, but don't close the body/html
        return mainTemplate.Substring(0, page16Index);
    }

    /// <summary>
    /// Loads a part template file
    /// </summary>
    private async Task<string> LoadPartTemplateAsync(string templatesDir, string fileName)
    {
        var path = Path.Combine(templatesDir, fileName);
        if (File.Exists(path))
        {
            return await File.ReadAllTextAsync(path);
        }

        _logger.LogWarning("Part template not found: {Path}", path);
        return "";
    }

    /// <summary>
    /// Replaces common brand placeholders in HTML
    /// </summary>
    private string ReplaceBrandPlaceholders(string html, string companyName, string? companyLogoUrl,
        string? coverImageUrl, string? primaryColor, string? secondaryColor, string? tertiaryColor)
    {
        html = html.Replace("{{PRIMARY_COLOR}}", primaryColor ?? "#0455A4");
        html = html.Replace("{{SECONDARY_COLOR}}", secondaryColor ?? "#1D8F6C");
        html = html.Replace("{{TERTIARY_COLOR}}", tertiaryColor ?? "#6C757D");
        html = html.Replace("{{COMPANY_NAME}}", EscapeHtml(companyName));
        html = html.Replace("{{COMPANY_LOGO}}", GetLogoHtml(companyLogoUrl));

        if (!string.IsNullOrEmpty(coverImageUrl))
        {
            html = html.Replace("{{COVER_IMAGE}}",
                $"<img src=\"{coverImageUrl}\" alt=\"Cover Image\" class=\"cover-image\" />");
        }
        else
        {
            html = html.Replace("{{COVER_IMAGE}}",
                "<div class=\"cover-image\" style=\"width: 100%; height: 200mm; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);\"></div>");
        }

        // Replace Ascend logo with base64 for PDF visibility
        var ascendLogoBase64 = GetAscendLogoBase64();
        if (!string.IsNullOrEmpty(ascendLogoBase64))
        {
            html = html.Replace("/logos/ascendevelopment_logo.jpeg", ascendLogoBase64);
        }

        return html;
    }

    /// <summary>
    /// Gets the HTML for the company logo
    /// </summary>
    private string GetLogoHtml(string? companyLogoUrl)
    {
        if (!string.IsNullOrEmpty(companyLogoUrl))
        {
            return $"<img src=\"{companyLogoUrl}\" alt=\"Company Logo\" class=\"client-logo\" />";
        }
        return "<div class=\"client-logo\" style=\"width: 80mm; height: 30mm; background: #f0f0f0; border: 1px solid #ddd;\"></div>";
    }

    /// <summary>
    /// Gets the Ascend logo as a base64 string
    /// </summary>
    private string GetAscendLogoBase64()
    {
        try
        {
            var templatesDir = Path.GetDirectoryName(_templatePath) ?? "";
            var logoPath = Path.Combine(templatesDir, "ascendevelopment_logo.jpeg");

            if (File.Exists(logoPath))
            {
                var bytes = File.ReadAllBytes(logoPath);
                var base64 = Convert.ToBase64String(bytes);
                return $"data:image/jpeg;base64,{base64}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Ascend logo for base64 embedding");
        }
        return "";
    }

    /// <summary>
    /// Generates a CLUSTER page from the template (shows competencies in table)
    /// </summary>
    private string GenerateClusterPage(string template, ClusterSummaryItem cluster,
        List<string> relationshipTypes, string? companyLogoUrl, string? chartImageUrl = null)
    {
        var html = template;

        html = html.Replace("{{COMPANY_LOGO}}", GetLogoHtml(companyLogoUrl));
        html = html.Replace("{{PAGE_TITLE}}", EscapeHtml(cluster.ClusterName));
        html = html.Replace("{{PAGE_SUBTITLE}}", ""); // No subtitle for cluster pages
        html = html.Replace("{{TABLE_SECTION_TITLE}}", "Dimensions Summary");
        html = html.Replace("{{TABLE_COLUMN_HEADER}}", "Dimensions");

        // Generate bar chart rows for cluster (aggregated scores)
        var barChartHtml = GenerateClusterBarChartRows(cluster, relationshipTypes);
        html = html.Replace("{{BAR_CHART_ROWS}}", barChartHtml);

        // Radial chart - use uploaded image or placeholder
        html = html.Replace("{{RADIAL_CHART_IMAGE}}", GetRadialChartHtml(chartImageUrl));

        // Generate table headers and rows (competencies within this cluster)
        var tableHeaders = GenerateDimensionsTableHeaders(relationshipTypes);
        var tableRows = GenerateClusterTableRows(cluster.Competencies, relationshipTypes);
        html = html.Replace("{{DIMENSIONS_TABLE_HEADERS}}", tableHeaders);
        html = html.Replace("{{DIMENSIONS_TABLE_ROWS}}", tableRows);

        return html;
    }

    /// <summary>
    /// Generates a COMPETENCY page from the template (shows questions/items in table)
    /// </summary>
    private string GenerateCompetencyDetailPage(string template, CompetencySummaryItem competency,
        List<string> relationshipTypes, string? companyLogoUrl, string? chartImageUrl = null)
    {
        var html = template;

        html = html.Replace("{{COMPANY_LOGO}}", GetLogoHtml(companyLogoUrl));
        html = html.Replace("{{PAGE_TITLE}}", EscapeHtml(competency.CompetencyName));
        html = html.Replace("{{PAGE_SUBTITLE}}",
            $"<p class=\"cluster-subtitle\" style=\"color: #666; font-size: 14px; margin-top: -10px;\">Cluster: {EscapeHtml(competency.ClusterName)}</p>");
        html = html.Replace("{{TABLE_SECTION_TITLE}}", "Item Level Feedback");
        html = html.Replace("{{TABLE_COLUMN_HEADER}}", "Questions");

        // Generate bar chart rows for competency
        var barChartHtml = GenerateCompetencyBarChartRows(competency, relationshipTypes);
        html = html.Replace("{{BAR_CHART_ROWS}}", barChartHtml);

        // Radial chart - use uploaded image or placeholder
        html = html.Replace("{{RADIAL_CHART_IMAGE}}", GetRadialChartHtml(chartImageUrl));

        // Generate table headers and rows (questions within this competency)
        var tableHeaders = GenerateDimensionsTableHeaders(relationshipTypes);
        var tableRows = GenerateCompetencyTableRows(competency.Questions, relationshipTypes);
        html = html.Replace("{{DIMENSIONS_TABLE_HEADERS}}", tableHeaders);
        html = html.Replace("{{DIMENSIONS_TABLE_ROWS}}", tableRows);

        return html;
    }

    /// <summary>
    /// Gets the HTML for a radial chart - either an uploaded image or a placeholder
    /// </summary>
    private string GetRadialChartHtml(string? chartImageUrl)
    {
        if (!string.IsNullOrEmpty(chartImageUrl))
        {
            return $"<img src=\"{chartImageUrl}\" alt=\"Radial Chart\" style=\"width: 200px; height: 200px; object-fit: contain;\" />";
        }
        return "<div style=\"width: 200px; height: 200px; border: 2px dashed #ccc; display: flex; align-items: center; justify-content: center; border-radius: 50%;\">Radial Chart</div>";
    }

    /// <summary>
    /// Gets the HTML for a page image (Page 4 or Page 6) - either an uploaded image or a placeholder
    /// </summary>
    private string GetPageImageHtml(string? imageUrl, string altText)
    {
        if (!string.IsNullOrEmpty(imageUrl))
        {
            return $"<div class=\"framework-image\"><img src=\"{imageUrl}\" alt=\"{altText}\" style=\"max-width: 100%; height: auto;\" /></div>";
        }
        return $"<div class=\"framework-image-placeholder\"><span>IMAGE PLACEHOLDER</span></div>";
    }

    private string GenerateClusterBarChartRows(ClusterSummaryItem cluster, List<string> relationshipTypes)
    {
        var sb = new StringBuilder();
        const double maxScore = 4.0;

        // ====== START: Updated bar chart rows with consistent bar-value alignment ======
        // Self bar - always show
        var selfScore = cluster.SelfScore ?? 0;
        var selfWidth = selfScore / maxScore * 100;
        sb.AppendLine($@"<div class=""bar-row"">
            <div class=""bar-label"">Self</div>
            <div class=""bar-track"">
                <div class=""bar-fill self-bar"" style=""width: {selfWidth:F1}%;""></div>
            </div>
            <span class=""bar-value"">{selfScore:F2}</span>
        </div>");

        // Others bar - always show
        var othersScore = cluster.OthersScore ?? 0;
        var othersWidth = othersScore / maxScore * 100;
        sb.AppendLine($@"<div class=""bar-row"">
            <div class=""bar-label"">Others</div>
            <div class=""bar-track"">
                <div class=""bar-fill others-bar"" style=""width: {othersWidth:F1}%;""></div>
            </div>
            <span class=""bar-value"">{othersScore:F2}</span>
        </div>");

        // Relationship-specific bars - show all, even with 0 value
        foreach (var rel in relationshipTypes)
        {
            var score = cluster.RelationshipScores.TryGetValue(rel, out var s) && s.HasValue ? s.Value : 0;
            var width = score / maxScore * 100;
            sb.AppendLine($@"<div class=""bar-row"">
            <div class=""bar-label"">{EscapeHtml(rel)}</div>
            <div class=""bar-track"">
                <div class=""bar-fill others-bar"" style=""width: {width:F1}%;""></div>
            </div>
            <span class=""bar-value"">{score:F2}</span>
        </div>");
        }
        // ====== END: Updated bar chart rows with consistent bar-value alignment ======

        _logger.LogDebug("Generated cluster bar chart for {ClusterName}: Self={SelfScore}, Others={OthersScore}, Relationships={RelScores}",
            cluster.ClusterName, cluster.SelfScore, cluster.OthersScore,
            string.Join(", ", cluster.RelationshipScores.Select(r => $"{r.Key}:{r.Value}")));

        return sb.ToString();
    }

    private string GenerateCompetencyBarChartRows(CompetencySummaryItem competency, List<string> relationshipTypes)
    {
        var sb = new StringBuilder();
        const double maxScore = 4.0;

        // ====== START: Updated competency bar chart rows with consistent bar-value alignment ======
        // Self bar - always show
        var selfScore = competency.SelfScore ?? 0;
        var selfWidth = selfScore / maxScore * 100;
        sb.AppendLine($@"<div class=""bar-row"">
            <div class=""bar-label"">Self</div>
            <div class=""bar-track"">
                <div class=""bar-fill self-bar"" style=""width: {selfWidth:F1}%;""></div>
            </div>
            <span class=""bar-value"">{selfScore:F2}</span>
        </div>");

        // Others bar - always show
        var othersScore = competency.OthersScore ?? 0;
        var othersWidth = othersScore / maxScore * 100;
        sb.AppendLine($@"<div class=""bar-row"">
            <div class=""bar-label"">Others</div>
            <div class=""bar-track"">
                <div class=""bar-fill others-bar"" style=""width: {othersWidth:F1}%;""></div>
            </div>
            <span class=""bar-value"">{othersScore:F2}</span>
        </div>");

        // Relationship-specific bars - show all, even with 0 value
        foreach (var rel in relationshipTypes)
        {
            var score = competency.RelationshipScores.TryGetValue(rel, out var s) && s.HasValue ? s.Value : 0;
            var width = score / maxScore * 100;
            sb.AppendLine($@"<div class=""bar-row"">
            <div class=""bar-label"">{EscapeHtml(rel)}</div>
            <div class=""bar-track"">
                <div class=""bar-fill others-bar"" style=""width: {width:F1}%;""></div>
            </div>
            <span class=""bar-value"">{score:F2}</span>
        </div>");
        }
        // ====== END: Updated competency bar chart rows with consistent bar-value alignment ======

        _logger.LogDebug("Generated competency bar chart for {CompetencyName}: Self={SelfScore}, Others={OthersScore}, Relationships={RelScores}",
            competency.CompetencyName, competency.SelfScore, competency.OthersScore,
            string.Join(", ", competency.RelationshipScores.Select(r => $"{r.Key}:{r.Value}")));

        return sb.ToString();
    }

    private string GenerateClusterTableRows(List<CompetencySummaryItem> competencies, List<string> relationshipTypes)
    {
        var sb = new StringBuilder();
        foreach (var comp in competencies)
        {
            sb.Append("<tr>");
            sb.Append($"<td class=\"td-dim\">{EscapeHtml(comp.CompetencyName)}</td>");
            sb.Append($"<td>{FormatRaterGroupScore(comp.SelfScore)}</td>");
            sb.Append($"<td>{FormatRaterGroupScore(comp.OthersScore)}</td>");

            foreach (var rel in relationshipTypes)
            {
                var score = comp.RelationshipScores.TryGetValue(rel, out var s) ? s : null;
                sb.Append($"<td>{FormatRaterGroupScore(score)}</td>");
            }
            sb.Append("</tr>");
        }
        return sb.ToString();
    }

    private string GenerateCompetencyTableRows(List<QuestionSummaryItem> questions, List<string> relationshipTypes)
    {
        var sb = new StringBuilder();
        foreach (var question in questions)
        {
            sb.Append("<tr>");
            sb.Append($"<td class=\"td-dim\">{EscapeHtml(CleanQuestionText(question.QuestionText))}</td>");
            sb.Append($"<td>{FormatRaterGroupScore(question.SelfScore)}</td>");
            sb.Append($"<td>{FormatRaterGroupScore(question.OthersScore)}</td>");

            foreach (var rel in relationshipTypes)
            {
                var score = question.RelationshipScores.TryGetValue(rel, out var s) ? s : null;
                sb.Append($"<td>{FormatRaterGroupScore(score)}</td>");
            }
            sb.Append("</tr>");
        }
        return sb.ToString();
    }

    private string GenerateBarChartRows(RaterGroupSummaryItem competency, List<string> relationshipTypes)
    {
        var sb = new StringBuilder();
        const double maxScore = 4.0; // Scale for bar width calculation

        // Self bar
        var selfWidth = competency.SelfScore.HasValue ? (competency.SelfScore.Value / maxScore * 100) : 0;
        sb.AppendLine($@"<div class=""bar-row"">
            <div class=""bar-label"">Self</div>
            <div class=""bar-track"">
                <div class=""bar-fill self-bar"" style=""width: {selfWidth:F1}%;""></div>
            </div>
        </div>");

        // Others bar
        var othersWidth = competency.OthersScore.HasValue ? (competency.OthersScore.Value / maxScore * 100) : 0;
        sb.AppendLine($@"<div class=""bar-row"">
            <div class=""bar-label"">Others</div>
            <div class=""bar-track"">
                <div class=""bar-fill others-bar"" style=""width: {othersWidth:F1}%;""></div>
            </div>
        </div>");

        // Relationship-specific bars
        foreach (var rel in relationshipTypes)
        {
            if (competency.RelationshipScores.TryGetValue(rel, out var score) && score.HasValue)
            {
                var width = score.Value / maxScore * 100;
                sb.AppendLine($@"<div class=""bar-row"">
                    <div class=""bar-label"">{EscapeHtml(rel)}</div>
                    <div class=""bar-track"">
                        <div class=""bar-fill others-bar"" style=""width: {width:F1}%;""></div>
                    </div>
                </div>");
            }
        }

        return sb.ToString();
    }

    private string GenerateDimensionsTableHeaders(List<string> relationshipTypes)
    {
        var sb = new StringBuilder();
        foreach (var rel in relationshipTypes)
        {
            sb.AppendLine($"<th class=\"col-score\">{EscapeHtml(rel)}</th>");
        }
        return sb.ToString();
    }

    private string GenerateDimensionsTableRows(RaterGroupSummaryItem competency, List<string> relationshipTypes)
    {
        var sb = new StringBuilder();
        sb.Append("<tr>");
        sb.Append($"<td class=\"td-dim\">{EscapeHtml(competency.CompetencyName)}</td>");
        sb.Append($"<td>{FormatRaterGroupScore(competency.SelfScore)}</td>");
        sb.Append($"<td>{FormatRaterGroupScore(competency.OthersScore)}</td>");

        foreach (var rel in relationshipTypes)
        {
            var score = competency.RelationshipScores.TryGetValue(rel, out var s) ? s : null;
            sb.Append($"<td>{FormatRaterGroupScore(score)}</td>");
        }
        sb.Append("</tr>");
        return sb.ToString();
    }

    /// <summary>
    /// Generates the feedback section with overflow handling
    /// </summary>
    private string GenerateFeedbackSection(string template,
        List<OpenEndedFeedbackItem> feedbackItems, string? companyLogoUrl)
    {
        var html = template;

        // Get the logo HTML for replacement
        var logoHtml = GetLogoHtml(companyLogoUrl);

        // Generate feedback items HTML (pass logo for new pages)
        var feedbackHtml = GenerateOpenEndedFeedbackHtml(feedbackItems, logoHtml);
        html = html.Replace("{{OPEN_ENDED_FEEDBACK_ITEMS}}", feedbackHtml);

        // Replace company logo placeholder in the first page
        html = html.Replace("{{COMPANY_LOGO}}", logoHtml);

        return html;
    }

    private string GenerateOpenEndedFeedbackHtml(List<OpenEndedFeedbackItem> feedbackItems, string logoHtml)
    {
        var sb = new StringBuilder();

        // Group by question
        var groupedByQuestion = feedbackItems
            .GroupBy(f => f.QuestionText)
            .ToList();

        // Footer HTML for new pages
        const string footerHtml = @"
    <div class=""page-footer"">
        <img src=""/logos/ascendevelopment_logo.jpeg"" alt=""Ascend Logo"" class=""ascend-logo"" />
        <div class=""page-number"">Page <span class=""page-number-index""></span> of <span class=""page-number-total""></span></div>
    </div>";

        // Content height budget in "units" - each unit represents roughly one line of text
        // A4 page with header/footer leaves approximately 700 units of usable content height
        const int pageHeightBudget = 700;
        const int firstPageTitleOverhead = 120; // "Feedback" title + intro text takes extra space
        const int questionTitleHeight = 60;     // Question title with some margin
        const int responseBaseHeight = 30;      // Base height for a response item
        const int charsPerLine = 100;           // Approximate characters that fit per line
        const int lineHeight = 18;              // Height per line of text
        const int dividerAndGuideHeight = 100;  // Divider + "Guide for Interpretation" section

        int currentPageBudget = pageHeightBudget - firstPageTitleOverhead;

        for (int i = 0; i < groupedByQuestion.Count; i++)
        {
            var group = groupedByQuestion[i];
            bool isLastQuestion = i == groupedByQuestion.Count - 1;

            // Question title with keyword bolding
            var questionTitle = FormatQuestionTitleWithBold(CleanQuestionText(group.Key));

            // Responses - sort by relationship priority: Manager first, then others
            var orderedResponses = group
                .OrderBy(r => GetRelationshipPriority(r.RaterType))
                .ToList();

            int responseIndex = 0;
            bool isFirstPageOfQuestion = true;

            while (responseIndex < orderedResponses.Count)
            {
                bool isLastPageOfQuestion = false;

                // Calculate space needed for question title
                int spaceNeeded = questionTitleHeight;

                // Collect responses that fit on this page
                var responsesForThisPage = new List<(int index, OpenEndedFeedbackItem item, int height)>();

                for (int r = responseIndex; r < orderedResponses.Count; r++)
                {
                    var item = orderedResponses[r];
                    // Calculate response height based on text length
                    int textLength = item.ResponseText?.Length ?? 0;
                    int lines = Math.Max(1, (int)Math.Ceiling((double)textLength / charsPerLine));
                    int responseHeight = responseBaseHeight + (lines - 1) * lineHeight;

                    // Check if this is potentially the last response (need space for divider+guide)
                    bool isLastResponse = r == orderedResponses.Count - 1;
                    int extraSpaceNeeded = isLastResponse ? dividerAndGuideHeight : 0;

                    if (spaceNeeded + responseHeight + extraSpaceNeeded <= currentPageBudget)
                    {
                        responsesForThisPage.Add((r + 1, item, responseHeight)); // r+1 for 1-based numbering
                        spaceNeeded += responseHeight;
                    }
                    else
                    {
                        // No more responses fit on this page
                        break;
                    }
                }

                // If no responses fit (shouldn't happen with reasonable budget), force at least one
                if (responsesForThisPage.Count == 0 && responseIndex < orderedResponses.Count)
                {
                    var item = orderedResponses[responseIndex];
                    responsesForThisPage.Add((responseIndex + 1, item, responseBaseHeight));
                }

                // Check if this is the last page of the question
                int lastResponseIndex = responseIndex + responsesForThisPage.Count;
                isLastPageOfQuestion = lastResponseIndex >= orderedResponses.Count;

                // Generate HTML for this page's content
                sb.AppendLine("<div class=\"feedback-question-group\">");

                // Show question title on first page, or a continuation indicator on subsequent pages
                if (isFirstPageOfQuestion)
                {
                    sb.AppendLine($"<div class=\"feedback-question-title\">{questionTitle}</div>");
                }
                else
                {
                    sb.AppendLine($"<div class=\"feedback-question-title\">{questionTitle} <span style=\"font-size: 10pt; color: #666;\">(continued)</span></div>");
                }

                // Responses for this page
                sb.AppendLine("<div class=\"feedback-responses-list\">");
                foreach (var (idx, item, _) in responsesForThisPage)
                {
                    sb.AppendLine($"<div class=\"feedback-response-item\">{idx}. {EscapeHtml(item.ResponseText)}</div>");
                }
                sb.AppendLine("</div>");

                // Only show divider and guide on the last page of the question
                if (isLastPageOfQuestion)
                {
                    sb.AppendLine("<div class=\"feedback-divider\"></div>");
                    sb.AppendLine(GenerateGuideForInterpretation());
                }

                sb.AppendLine("</div>"); // Close feedback-question-group

                // Move to next batch of responses
                responseIndex = lastResponseIndex;
                isFirstPageOfQuestion = false;

                // Add page break if not the last page of the last question
                bool needsPageBreak = !(isLastQuestion && isLastPageOfQuestion);
                if (needsPageBreak)
                {
                    sb.AppendLine("        </div>"); // Close open-ended-content
                    sb.AppendLine("    </div>"); // Close page-content
                    sb.AppendLine(footerHtml);
                    sb.AppendLine("</div>"); // Close current page

                    // Start new page
                    sb.AppendLine("<div class=\"page\">");
                    sb.AppendLine($"    <div class=\"page-header\">{logoHtml}</div>");
                    sb.AppendLine("    <div class=\"page-content respondent-summary-page\">");
                    sb.AppendLine("        <div class=\"open-ended-content\">"); // Reopen open-ended-content for new page

                    // Reset page budget for new page (no first page overhead)
                    currentPageBudget = pageHeightBudget;
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format question title to bold keywords like "stop doing", "start doing", "continue doing"
    /// </summary>
    private string FormatQuestionTitleWithBold(string questionText)
    {
        if (string.IsNullOrEmpty(questionText)) return questionText;

        var escaped = EscapeHtml(questionText);

        // Bold common keywords
        var keywords = new[] { "stop doing", "start doing", "continue doing", "strengths", "weaknesses", "improve", "development" };
        foreach (var keyword in keywords)
        {
            var pattern = $"(?i){System.Text.RegularExpressions.Regex.Escape(keyword)}";
            escaped = System.Text.RegularExpressions.Regex.Replace(
                escaped,
                pattern,
                match => $"<strong>{match.Value}</strong>");
        }

        return escaped;
    }

    /// <summary>
    /// Get relationship priority for ordering (Manager first)
    /// </summary>
    private int GetRelationshipPriority(string relationship)
    {
        return relationship?.ToLower() switch
        {
            "manager" or "line-manager" or "linemanager" => 1,
            "self" => 2,
            "direct" or "directreport" or "direct-report" => 3,
            "peer" => 4,
            "stakeholder" => 5,
            "skipline" or "skip-line" => 6,
            _ => 10
        };
    }

    /// <summary>
    /// Generate the "Guide for Interpretation" HTML box
    /// </summary>
    private string GenerateGuideForInterpretation()
    {
        return @"
<div class=""guide-interpretation"">
    <h2 class=""guide-interpretation-title"">Guide for Interpretation:</h2>
    <p class=""guide-interpretation-content"">
        <li>Open-ended feedback shall explain the reasons of ratings and similarity and differences in the ratings, observed in other sections of the report.</li>
        <li>Compare the comments to your own notes pertinent to your strengths and development areas.</li>
        <li>It is important to look for recurring themes in the feedback.</li>
        <li>You may have already known about some of the feedback provided while some of it may appear as a surprise and an insight for you to delve deeper into.</li>
    </p>
</div>";
    }

private string CleanQuestionText(string text)
{
    if (string.IsNullOrEmpty(text)) return text;

    // 1. Remove the specific placeholder (case-insensitive)
    // You can add more placeholders to this list if needed
    string cleaned = text.Replace("{subjectName}", "", StringComparison.OrdinalIgnoreCase)
                         .Replace("{subject}", "", StringComparison.OrdinalIgnoreCase);

    // 2. Trim whitespace (removes the leading space left by the placeholder)
    cleaned = cleaned.Trim();

    // 3. Capitalize the first letter if the text isn't empty
    if (cleaned.Length > 0)
    {
        cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);
    }

    return cleaned;
}

    #endregion
}
