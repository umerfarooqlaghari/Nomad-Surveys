using System.ComponentModel.DataAnnotations;

namespace Alpha.Api.DTOs.Request;

public class AssignSurveyRequest
{
    [Required]
    public List<Guid> SubjectEvaluatorIds { get; set; } = new();
}

public class UnassignSurveyRequest
{
    [Required]
    public List<Guid> SubjectEvaluatorIds { get; set; } = new();
}

