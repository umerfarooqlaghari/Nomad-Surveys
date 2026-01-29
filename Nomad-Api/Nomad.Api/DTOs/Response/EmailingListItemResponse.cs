using System;
using System.Collections.Generic;

namespace Nomad.Api.DTOs.Response;

public class EmailingListItemResponse
{
    public Guid SurveyId { get; set; }
    public string SurveyName { get; set; } = string.Empty;
    public Guid EvaluatorId { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
    public string EvaluatorEmail { get; set; } = string.Empty;
    public int SubjectCount { get; set; }
    public List<string> SubjectNames { get; set; } = new();
    public DateTime? LastReminderSentAt { get; set; }
    public DateTime? AssignmentEmailSentAt { get; set; }
    public List<Guid> SubjectEvaluatorSurveyIds { get; set; } = new();
}
