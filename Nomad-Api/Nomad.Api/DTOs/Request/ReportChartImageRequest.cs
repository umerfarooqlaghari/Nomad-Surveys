using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

/// <summary>
/// Request DTO for creating/updating a report chart image
/// </summary>
public class CreateReportChartImageRequest
{
    /// <summary>
    /// Type of image: "Page4", "Page6", "Cluster", "Competency"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ImageType { get; set; } = string.Empty;

    /// <summary>
    /// Cluster name (required for Cluster and Competency types)
    /// </summary>
    [MaxLength(255)]
    public string? ClusterName { get; set; }

    /// <summary>
    /// Competency name (required for Competency type only)
    /// </summary>
    [MaxLength(255)]
    public string? CompetencyName { get; set; }

    /// <summary>
    /// URL to the uploaded image
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating a report chart image
/// </summary>
public class UpdateReportChartImageRequest
{
    /// <summary>
    /// URL to the uploaded image
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string ImageUrl { get; set; } = string.Empty;
}

