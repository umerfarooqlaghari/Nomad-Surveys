namespace Nomad.Api.DTOs.Response;

public class SurveyAssignmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public int UnassignedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class AssignedRelationshipResponse
{
    public Guid Id { get; set; }
    public Guid SubjectEvaluatorId { get; set; }
    public string? Relationship { get; set; }
    
    // Subject details
    public Guid SubjectId { get; set; }
    public string SubjectFullName { get; set; } = string.Empty;
    public string SubjectEmail { get; set; } = string.Empty;
    public string SubjectEmployeeIdString { get; set; } = string.Empty;
    public string? SubjectDesignation { get; set; }
    
    // Evaluator details
    public Guid EvaluatorId { get; set; }
    public string EvaluatorFullName { get; set; } = string.Empty;
    public string EvaluatorEmail { get; set; } = string.Empty;
    public string EvaluatorEmployeeIdString { get; set; } = string.Empty;
    public string? EvaluatorDesignation { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AvailableRelationshipResponse
{
    public Guid SubjectEvaluatorId { get; set; }
    public string? Relationship { get; set; }
    
    // Subject details
    public Guid SubjectId { get; set; }
    public string SubjectFullName { get; set; } = string.Empty;
    public string SubjectEmail { get; set; } = string.Empty;
    public string SubjectEmployeeIdString { get; set; } = string.Empty;
    public string? SubjectDesignation { get; set; }
    
    // Evaluator details
    public Guid EvaluatorId { get; set; }
    public string EvaluatorFullName { get; set; } = string.Empty;
    public string EvaluatorEmail { get; set; } = string.Empty;
    public string EvaluatorEmployeeIdString { get; set; } = string.Empty;
    public string? EvaluatorDesignation { get; set; }
    
    public bool IsActive { get; set; }
}

public class RelationshipWithSurveysResponse
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid EvaluatorId { get; set; }
    public string? Relationship { get; set; }
    public string SubjectFullName { get; set; } = string.Empty;
    public string EvaluatorFullName { get; set; } = string.Empty;
    public List<SurveyAssignmentInfo> SurveyAssignments { get; set; } = new();
}

public class SurveyAssignmentInfo
{
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
}

