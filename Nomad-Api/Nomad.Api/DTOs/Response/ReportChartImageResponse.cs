namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for a report chart image
/// </summary>
public class ReportChartImageResponse
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public string ImageType { get; set; } = string.Empty;
    public string? ClusterName { get; set; }
    public string? CompetencyName { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Hierarchical response for folder-based chart image view
/// </summary>
public class ChartImageHierarchyResponse
{
    /// <summary>
    /// Page 4 image (optional)
    /// </summary>
    public ReportChartImageResponse? Page4Image { get; set; }

    /// <summary>
    /// Page 6 image (optional)
    /// </summary>
    public ReportChartImageResponse? Page6Image { get; set; }

    /// <summary>
    /// Clusters with their competencies
    /// </summary>
    public List<ClusterChartImagesResponse> Clusters { get; set; } = new();
}

/// <summary>
/// Cluster with its chart image and competencies
/// </summary>
public class ClusterChartImagesResponse
{
    public string ClusterName { get; set; } = string.Empty;
    public ReportChartImageResponse? ClusterImage { get; set; }
    public List<CompetencyChartImageResponse> Competencies { get; set; } = new();
}

/// <summary>
/// Competency with its chart image
/// </summary>
public class CompetencyChartImageResponse
{
    public string CompetencyName { get; set; } = string.Empty;
    public ReportChartImageResponse? Image { get; set; }
}

