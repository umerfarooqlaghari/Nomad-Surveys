namespace Nomad.Api.DTOs.Response;

/// <summary>
/// Response DTO for participant dashboard
/// </summary>
public class ParticipantDashboardResponse
{
    public DashboardStats Stats { get; set; } = new();
    public List<PendingEvaluationDto> PendingEvaluations { get; set; } = new();
}

/// <summary>
/// Dashboard statistics
/// </summary>
public class DashboardStats
{
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalAssigned { get; set; }
}

/// <summary>
/// Pending evaluation DTO for dashboard
/// </summary>
public class PendingEvaluationDto
{
    public Guid AssignmentId { get; set; }
    public Guid SurveyId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SurveyTitle { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

