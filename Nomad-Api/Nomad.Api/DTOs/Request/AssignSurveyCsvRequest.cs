using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

public class AssignSurveyCsvRequest
{
    [Required]
    [MinLength(1)]
    public List<AssignSurveyCsvRow> Rows { get; set; } = new();
}

public class AssignSurveyCsvRow
{
    [Required]
    public string EvaluatorId { get; set; } = string.Empty;

    [Required]
    public string SubjectId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Relationship { get; set; } = string.Empty;
}




