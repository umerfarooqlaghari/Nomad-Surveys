using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Authorization;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/surveys/{surveyId}/chart-images")]
[AuthorizeTenant]
public class ReportChartImagesController : ControllerBase
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<ReportChartImagesController> _logger;

    public ReportChartImagesController(
        NomadSurveysDbContext context,
        ILogger<ReportChartImagesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all chart images for a survey
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ReportChartImageResponse>>> GetChartImages(Guid surveyId)
    {
        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Unauthorized("Tenant ID not found");

        var images = await _context.ReportChartImages
            .Where(i => i.SurveyId == surveyId && i.TenantId == tenantId.Value)
            .OrderBy(i => i.ImageType)
            .ThenBy(i => i.ClusterName)
            .ThenBy(i => i.CompetencyName)
            .Select(i => new ReportChartImageResponse
            {
                Id = i.Id,
                SurveyId = i.SurveyId,
                ImageType = i.ImageType,
                ClusterName = i.ClusterName,
                CompetencyName = i.CompetencyName,
                ImageUrl = i.ImageUrl,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            })
            .ToListAsync();

        return Ok(images);
    }

    /// <summary>
    /// Get chart images in hierarchical folder structure
    /// Returns all clusters and competencies for the tenant, with any uploaded images attached
    /// </summary>
    [HttpGet("hierarchy")]
    public async Task<ActionResult<ChartImageHierarchyResponse>> GetChartImagesHierarchy(Guid surveyId)
    {
        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Unauthorized("Tenant ID not found");

        _logger.LogInformation("Getting chart images hierarchy for survey {SurveyId}, tenant {TenantId}",
            surveyId, tenantId.Value);

        // Get all uploaded images for this survey
        var images = await _context.ReportChartImages
            .Where(i => i.SurveyId == surveyId && i.TenantId == tenantId.Value)
            .ToListAsync();

        _logger.LogInformation("Found {ImageCount} chart images for survey", images.Count);

        // Get all clusters and competencies for this tenant (query filter already applied)
        var clusters = await _context.Clusters
            .Include(c => c.Competencies.Where(comp => comp.IsActive))
            .Where(c => c.IsActive)
            .OrderBy(c => c.ClusterName)
            .ToListAsync();

        _logger.LogInformation("Found {ClusterCount} clusters for tenant", clusters.Count);

        var response = new ChartImageHierarchyResponse
        {
            Page4Image = MapToResponse(images.FirstOrDefault(i => i.ImageType == "Page4")),
            Page6Image = MapToResponse(images.FirstOrDefault(i => i.ImageType == "Page6"))
        };

        // Build cluster hierarchy with actual clusters from database
        foreach (var cluster in clusters)
        {
            var clusterImage = images.FirstOrDefault(i =>
                i.ImageType == "Cluster" && i.ClusterName == cluster.ClusterName);

            var clusterResponse = new ClusterChartImagesResponse
            {
                ClusterName = cluster.ClusterName,
                ClusterImage = MapToResponse(clusterImage)
            };

            // Get competencies for this cluster (already filtered to active ones in Include)
            var competencies = cluster.Competencies
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var competency in competencies)
            {
                var competencyImage = images.FirstOrDefault(i =>
                    i.ImageType == "Competency" &&
                    i.ClusterName == cluster.ClusterName &&
                    i.CompetencyName == competency.Name);

                clusterResponse.Competencies.Add(new CompetencyChartImageResponse
                {
                    CompetencyName = competency.Name,
                    Image = MapToResponse(competencyImage)
                });
            }

            response.Clusters.Add(clusterResponse);
        }

        return Ok(response);
    }

    /// <summary>
    /// Create or update a chart image
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReportChartImageResponse>> CreateOrUpdateChartImage(
        Guid surveyId,
        [FromBody] CreateReportChartImageRequest request)
    {
        try
        {
            _logger.LogInformation(
                "CreateOrUpdateChartImage called for survey {SurveyId}, imageType={ImageType}, cluster={ClusterName}, competency={CompetencyName}, url={ImageUrl}",
                surveyId, request.ImageType, request.ClusterName, request.CompetencyName, request.ImageUrl?.Substring(0, Math.Min(50, request.ImageUrl?.Length ?? 0)));

            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
                return Unauthorized("Tenant ID not found");

            // Validate survey exists
            var surveyExists = await _context.Surveys.AnyAsync(s => s.Id == surveyId && s.TenantId == tenantId.Value);
            if (!surveyExists)
                return NotFound("Survey not found");

            // Check if image already exists for this type/cluster/competency
            var existing = await _context.ReportChartImages.FirstOrDefaultAsync(i =>
                i.SurveyId == surveyId &&
                i.TenantId == tenantId.Value &&
                i.ImageType == request.ImageType &&
                i.ClusterName == request.ClusterName &&
                i.CompetencyName == request.CompetencyName);

            if (existing != null)
            {
                // Update existing
                _logger.LogInformation("Updating existing chart image {ImageId}", existing.Id);
                existing.ImageUrl = request.ImageUrl;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(MapToResponse(existing));
            }

            // Create new
            var image = new ReportChartImage
            {
                Id = Guid.NewGuid(),
                SurveyId = surveyId,
                TenantId = tenantId.Value,
                ImageType = request.ImageType,
                ClusterName = request.ClusterName,
                CompetencyName = request.CompetencyName,
                ImageUrl = request.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Creating new chart image {ImageId}", image.Id);
            _context.ReportChartImages.Add(image);
            await _context.SaveChangesAsync();

            // Return Ok instead of CreatedAtAction to avoid route value issues with tenantSlug
            return Ok(MapToResponse(image));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating chart image for survey {SurveyId}", surveyId);
            return StatusCode(500, new { error = "Failed to save chart image", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a chart image
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteChartImage(Guid surveyId, Guid id)
    {
        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Unauthorized("Tenant ID not found");

        var image = await _context.ReportChartImages.FirstOrDefaultAsync(i =>
            i.Id == id && i.SurveyId == surveyId && i.TenantId == tenantId.Value);

        if (image == null)
            return NotFound("Chart image not found");

        _context.ReportChartImages.Remove(image);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete chart image by type/cluster/competency
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult> DeleteChartImageByType(
        Guid surveyId,
        [FromQuery] string imageType,
        [FromQuery] string? clusterName = null,
        [FromQuery] string? competencyName = null)
    {
        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Unauthorized("Tenant ID not found");

        var image = await _context.ReportChartImages.FirstOrDefaultAsync(i =>
            i.SurveyId == surveyId &&
            i.TenantId == tenantId.Value &&
            i.ImageType == imageType &&
            i.ClusterName == clusterName &&
            i.CompetencyName == competencyName);

        if (image == null)
            return NotFound("Chart image not found");

        _context.ReportChartImages.Remove(image);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static ReportChartImageResponse? MapToResponse(ReportChartImage? image)
    {
        if (image == null) return null;
        return new ReportChartImageResponse
        {
            Id = image.Id,
            SurveyId = image.SurveyId,
            ImageType = image.ImageType,
            ClusterName = image.ClusterName,
            CompetencyName = image.CompetencyName,
            ImageUrl = image.ImageUrl,
            CreatedAt = image.CreatedAt,
            UpdatedAt = image.UpdatedAt
        };
    }
}

