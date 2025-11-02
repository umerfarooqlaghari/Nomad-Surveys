using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Nomad.Api.Entities;

/// <summary>
/// Survey submission entity for storing participant responses
/// </summary>
public class SurveySubmission
{
    public Guid Id { get; set; }

    // Foreign keys
    public Guid SubjectEvaluatorSurveyId { get; set; }
    public Guid EvaluatorId { get; set; } // The evaluator who is filling out the survey
    public Guid SubjectId { get; set; } // The subject being evaluated
    public Guid SurveyId { get; set; }

    /// <summary>
    /// Survey response data stored as JSONB in PostgreSQL
    /// Contains the answers to all survey questions
    /// </summary>
    public JsonDocument? ResponseData { get; set; }

    /// <summary>
    /// Status of the survey submission
    /// Pending: Not started
    /// InProgress: Started but not completed (auto-saved)
    /// Completed: Submitted
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed

    // Timestamps
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual SubjectEvaluatorSurvey SubjectEvaluatorSurvey { get; set; } = null!;
    public virtual Evaluator Evaluator { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
    public virtual Survey Survey { get; set; } = null!;
    public virtual Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// Survey submission status enum
/// </summary>
public static class SurveySubmissionStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
}

