using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using System.Text.Json;

namespace Nomad.Api.Services;

/// <summary>
/// Service for participant portal operations
/// </summary>
public class ParticipantService : IParticipantService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<ParticipantService> _logger;

    public ParticipantService(
        NomadSurveysDbContext context,
        ILogger<ParticipantService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ParticipantDashboardResponse> GetDashboardAsync(Guid userId)
    {
        try
        {
            // Get user's employee ID
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return new ParticipantDashboardResponse();
            }

            if (user.EmployeeId == null)
            {
                _logger.LogWarning("User {UserId} (Email: {Email}) has no associated employee. This user cannot access participant features.",
                    userId, user.Email);
                return new ParticipantDashboardResponse();
            }

            _logger.LogInformation("Getting dashboard for user {UserId}, employee {EmployeeId}", userId, user.EmployeeId);

            // Get all evaluators that reference this employee
            var evaluatorIds = await _context.Evaluators
                .Where(e => e.EmployeeId == user.EmployeeId)
                .Select(e => e.Id)
                .ToListAsync();

            _logger.LogInformation("Found {Count} evaluator records for employee {EmployeeId}", evaluatorIds.Count, user.EmployeeId);

            // Get all survey assignments where this employee is the evaluator
            var assignments = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                .Include(ses => ses.Survey)
                .Where(ses => evaluatorIds.Contains(ses.SubjectEvaluator.EvaluatorId) && ses.IsActive)
                .ToListAsync();

            _logger.LogInformation("Found {Count} survey assignments for employee {EmployeeId}", assignments.Count, user.EmployeeId);

            // Get all submissions for this employee's evaluator records
            var submissions = await _context.SurveySubmissions
                .Where(ss => evaluatorIds.Contains(ss.EvaluatorId))
                .ToListAsync();

            // Calculate stats
            var stats = new DashboardStats
            {
                TotalAssigned = assignments.Count,
                CompletedCount = submissions.Count(s => s.Status == SurveySubmissionStatus.Completed),
                InProgressCount = submissions.Count(s => s.Status == SurveySubmissionStatus.InProgress),
                PendingCount = assignments.Count - submissions.Count(s => s.Status == SurveySubmissionStatus.Completed)
            };

            // Get pending evaluations (not completed)
            var completedAssignmentIds = submissions
                .Where(s => s.Status == SurveySubmissionStatus.Completed)
                .Select(s => s.SubjectEvaluatorSurveyId)
                .ToHashSet();

            var pendingEvaluations = assignments
                .Where(a => !completedAssignmentIds.Contains(a.Id))
                .OrderBy(a => a.CreatedAt)
                .Take(5)
                .Select(a => new PendingEvaluationDto
                {
                    AssignmentId = a.Id,
                    SurveyId = a.SurveyId,
                    SubjectName = $"{a.SubjectEvaluator.Subject.Employee.FirstName} {a.SubjectEvaluator.Subject.Employee.LastName}",
                    SurveyTitle = a.Survey.Title,
                    DueDate = null, // Can be added to SubjectEvaluatorSurvey entity if needed
                    Status = submissions.FirstOrDefault(s => s.SubjectEvaluatorSurveyId == a.Id)?.Status ?? SurveySubmissionStatus.Pending
                })
                .ToList();

            return new ParticipantDashboardResponse
            {
                Stats = stats,
                PendingEvaluations = pendingEvaluations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<AssignedEvaluationResponse>> GetAssignedEvaluationsAsync(Guid userId, string? status = null, string? search = null)
    {
        try
        {
            // Get user's employee ID
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return new List<AssignedEvaluationResponse>();
            }

            if (user.EmployeeId == null)
            {
                _logger.LogWarning("User {UserId} (Email: {Email}) has no associated employee. Cannot retrieve evaluations.",
                    userId, user.Email);
                return new List<AssignedEvaluationResponse>();
            }

            // Get all evaluators that reference this employee
            var evaluatorIds = await _context.Evaluators
                .Where(e => e.EmployeeId == user.EmployeeId)
                .Select(e => e.Id)
                .ToListAsync();

            if (!evaluatorIds.Any())
            {
                _logger.LogWarning("No evaluator records found for employee {EmployeeId}", user.EmployeeId);
                return new List<AssignedEvaluationResponse>();
            }

            // Get all survey assignments where this employee is the evaluator
            var query = _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                .Include(ses => ses.Survey)
                .Where(ses => evaluatorIds.Contains(ses.SubjectEvaluator.EvaluatorId) && ses.IsActive);

            var assignments = await query.ToListAsync();

            // Get submissions for these assignments
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await _context.SurveySubmissions
                .Where(ss => assignmentIds.Contains(ss.SubjectEvaluatorSurveyId))
                .ToListAsync();

            // Build response
            var result = assignments.Select(a =>
            {
                var submission = submissions.FirstOrDefault(s => s.SubjectEvaluatorSurveyId == a.Id);
                var subjectEmployee = a.SubjectEvaluator.Subject.Employee;

                return new AssignedEvaluationResponse
                {
                    AssignmentId = a.Id,
                    SurveyId = a.SurveyId,
                    SubjectId = a.SubjectEvaluator.SubjectId,
                    EvaluatorId = a.SubjectEvaluator.EvaluatorId,
                    SubjectName = $"{subjectEmployee.FirstName} {subjectEmployee.LastName}",
                    SubjectEmail = subjectEmployee.Email,
                    SubjectDepartment = subjectEmployee.Department ?? "",
                    SubjectDesignation = subjectEmployee.Designation ?? "",
                    SurveyTitle = a.Survey.Title,
                    SurveyDescription = a.Survey.Description ?? "",
                    IsSelfEvaluation = a.Survey.IsSelfEvaluation,
                    RelationshipType = a.SubjectEvaluator.Relationship,
                    Status = submission?.Status ?? SurveySubmissionStatus.Pending,
                    StartedAt = submission?.StartedAt,
                    CompletedAt = submission?.CompletedAt,
                    AssignedAt = a.CreatedAt,
                    DueDate = null // Can be added to SubjectEvaluatorSurvey entity if needed
                };
            }).ToList();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(status))
            {
                result = result.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                result = result.Where(r =>
                    r.SubjectName.ToLower().Contains(searchLower) ||
                    r.SurveyTitle.ToLower().Contains(searchLower) ||
                    r.SubjectDepartment.ToLower().Contains(searchLower)
                ).ToList();
            }

            return result.OrderByDescending(r => r.AssignedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned evaluations for user {UserId}", userId);
            throw;
        }
    }

    public async Task<EvaluationFormResponse?> GetEvaluationFormAsync(Guid userId, Guid assignmentId)
    {
        try
        {
            // Get user's employee ID
            var user = await _context.Users.FindAsync(userId);
            if (user?.EmployeeId == null)
            {
                return null;
            }

            // Get all evaluators that reference this employee
            var evaluatorIds = await _context.Evaluators
                .Where(e => e.EmployeeId == user.EmployeeId)
                .Select(e => e.Id)
                .ToListAsync();

            if (!evaluatorIds.Any())
            {
                return null;
            }

            // Get assignment with all related data
            var assignment = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                .Include(ses => ses.Survey)
                .FirstOrDefaultAsync(ses => ses.Id == assignmentId && evaluatorIds.Contains(ses.SubjectEvaluator.EvaluatorId));

            if (assignment == null)
            {
                _logger.LogWarning("Assignment {AssignmentId} not found for employee {EmployeeId}", assignmentId, user.EmployeeId);
                return null;
            }

            var evaluatorId = assignment.SubjectEvaluator.EvaluatorId;

            // Get or create submission
            var submission = await _context.SurveySubmissions
                .FirstOrDefaultAsync(ss => ss.SubjectEvaluatorSurveyId == assignmentId && ss.EvaluatorId == evaluatorId);

            var subjectEmployee = assignment.SubjectEvaluator.Subject.Employee;
            var evaluatorEmployee = assignment.SubjectEvaluator.Evaluator.Employee;

            // Replace placeholders in survey schema
            var surveySchema = ReplacePlaceholders(
                assignment.Survey.Schema,
                $"{subjectEmployee.FirstName} {subjectEmployee.LastName}",
                $"{evaluatorEmployee.FirstName} {evaluatorEmployee.LastName}"
            );

            return new EvaluationFormResponse
            {
                AssignmentId = assignment.Id,
                SurveyId = assignment.SurveyId,
                SubjectId = assignment.SubjectEvaluator.SubjectId,
                EvaluatorId = evaluatorId,
                SurveyTitle = assignment.Survey.Title,
                SurveyDescription = assignment.Survey.Description ?? "",
                SurveySchema = surveySchema,
                SubjectName = $"{subjectEmployee.FirstName} {subjectEmployee.LastName}",
                EvaluatorName = $"{evaluatorEmployee.FirstName} {evaluatorEmployee.LastName}",
                IsSelfEvaluation = assignment.Survey.IsSelfEvaluation,
                RelationshipType = assignment.SubjectEvaluator.Relationship,
                Status = submission?.Status ?? SurveySubmissionStatus.Pending,
                SavedResponseData = submission?.ResponseData,
                StartedAt = submission?.StartedAt,
                CompletedAt = submission?.CompletedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluation form for assignment {AssignmentId}", assignmentId);
            throw;
        }
    }

    public async Task<bool> SaveDraftAsync(Guid userId, Guid assignmentId, SaveDraftRequest request)
    {
        try
        {
            // Get user's employee ID
            var user = await _context.Users.FindAsync(userId);
            if (user?.EmployeeId == null)
            {
                return false;
            }

            // Get all evaluators that reference this employee
            var evaluatorIds = await _context.Evaluators
                .Where(e => e.EmployeeId == user.EmployeeId)
                .Select(e => e.Id)
                .ToListAsync();

            if (!evaluatorIds.Any())
            {
                return false;
            }

            // Verify assignment belongs to this employee's evaluator
            var assignment = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                .FirstOrDefaultAsync(ses => ses.Id == assignmentId && evaluatorIds.Contains(ses.SubjectEvaluator.EvaluatorId));

            if (assignment == null)
            {
                _logger.LogWarning("Assignment {AssignmentId} not found for employee {EmployeeId}", assignmentId, user.EmployeeId);
                return false;
            }

            var evaluatorId = assignment.SubjectEvaluator.EvaluatorId;

            // Get or create submission
            var submission = await _context.SurveySubmissions
                .FirstOrDefaultAsync(ss => ss.SubjectEvaluatorSurveyId == assignmentId && ss.EvaluatorId == evaluatorId);

            if (submission == null)
            {
                // Create new submission
                submission = new SurveySubmission
                {
                    Id = Guid.NewGuid(),
                    SubjectEvaluatorSurveyId = assignmentId,
                    EvaluatorId = evaluatorId,
                    SubjectId = assignment.SubjectEvaluator.SubjectId,
                    SurveyId = assignment.SurveyId,
                    ResponseData = request.ResponseData,
                    Status = SurveySubmissionStatus.InProgress,
                    StartedAt = DateTime.UtcNow,
                    TenantId = assignment.TenantId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SurveySubmissions.Add(submission);
            }
            else
            {
                // Update existing submission
                submission.ResponseData = request.ResponseData;
                submission.Status = SurveySubmissionStatus.InProgress;
                submission.UpdatedAt = DateTime.UtcNow;

                if (submission.StartedAt == null)
                {
                    submission.StartedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving draft for assignment {AssignmentId}", assignmentId);
            throw;
        }
    }

    public async Task<bool> SubmitEvaluationAsync(Guid userId, Guid assignmentId, SubmitEvaluationRequest request)
    {
        try
        {
            // Get user's employee ID
            var user = await _context.Users.FindAsync(userId);
            if (user?.EmployeeId == null)
            {
                return false;
            }

            // Get all evaluators that reference this employee
            var evaluatorIds = await _context.Evaluators
                .Where(e => e.EmployeeId == user.EmployeeId)
                .Select(e => e.Id)
                .ToListAsync();

            if (!evaluatorIds.Any())
            {
                return false;
            }

            // Verify assignment belongs to this employee's evaluator
            var assignment = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                .FirstOrDefaultAsync(ses => ses.Id == assignmentId && evaluatorIds.Contains(ses.SubjectEvaluator.EvaluatorId));

            if (assignment == null)
            {
                _logger.LogWarning("Assignment {AssignmentId} not found for employee {EmployeeId}", assignmentId, user.EmployeeId);
                return false;
            }

            var evaluatorId = assignment.SubjectEvaluator.EvaluatorId;

            // Get or create submission
            var submission = await _context.SurveySubmissions
                .FirstOrDefaultAsync(ss => ss.SubjectEvaluatorSurveyId == assignmentId && ss.EvaluatorId == evaluatorId);

            if (submission == null)
            {
                // Create new submission
                submission = new SurveySubmission
                {
                    Id = Guid.NewGuid(),
                    SubjectEvaluatorSurveyId = assignmentId,
                    EvaluatorId = evaluatorId,
                    SubjectId = assignment.SubjectEvaluator.SubjectId,
                    SurveyId = assignment.SurveyId,
                    ResponseData = request.ResponseData,
                    Status = SurveySubmissionStatus.Completed,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    TenantId = assignment.TenantId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SurveySubmissions.Add(submission);
            }
            else
            {
                // Update existing submission
                submission.ResponseData = request.ResponseData;
                submission.Status = SurveySubmissionStatus.Completed;
                submission.CompletedAt = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;

                if (submission.StartedAt == null)
                {
                    submission.StartedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting evaluation for assignment {AssignmentId}", assignmentId);
            throw;
        }
    }

    public async Task<List<SubmissionHistoryResponse>> GetSubmissionHistoryAsync(Guid userId, string? search = null)
    {
        try
        {
            // Get user's employee ID
            var user = await _context.Users.FindAsync(userId);
            if (user?.EmployeeId == null)
            {
                return new List<SubmissionHistoryResponse>();
            }

            // Get all evaluators that reference this employee
            var evaluatorIds = await _context.Evaluators
                .Where(e => e.EmployeeId == user.EmployeeId)
                .Select(e => e.Id)
                .ToListAsync();

            if (!evaluatorIds.Any())
            {
                return new List<SubmissionHistoryResponse>();
            }

            // Get all completed submissions for this employee's evaluators
            var query = _context.SurveySubmissions
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(ss => ss.Survey)
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                .Where(ss => evaluatorIds.Contains(ss.EvaluatorId) && ss.Status == SurveySubmissionStatus.Completed);

            var submissions = await query.ToListAsync();

            var result = submissions.Select(s =>
            {
                var subjectEmployee = s.Subject.Employee;
                return new SubmissionHistoryResponse
                {
                    SubmissionId = s.Id,
                    AssignmentId = s.SubjectEvaluatorSurveyId,
                    SurveyId = s.SurveyId,
                    SubjectName = $"{subjectEmployee.FirstName} {subjectEmployee.LastName}",
                    SubjectEmail = subjectEmployee.Email,
                    SubjectDepartment = subjectEmployee.Department ?? "",
                    SurveyTitle = s.Survey.Title,
                    IsSelfEvaluation = s.Survey.IsSelfEvaluation,
                    RelationshipType = s.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship,
                    CompletedAt = s.CompletedAt ?? DateTime.UtcNow,
                    SubmittedAt = s.CompletedAt ?? DateTime.UtcNow
                };
            }).ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                result = result.Where(r =>
                    r.SubjectName.ToLower().Contains(searchLower) ||
                    r.SurveyTitle.ToLower().Contains(searchLower) ||
                    r.SubjectDepartment.ToLower().Contains(searchLower)
                ).ToList();
            }

            return result.OrderByDescending(r => r.CompletedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SubmissionDetailResponse?> GetSubmissionDetailAsync(Guid userId, Guid submissionId)
    {
        try
        {
            // Get user's employee ID
            var user = await _context.Users.FindAsync(userId);
            if (user?.EmployeeId == null)
            {
                return null;
            }

            // Get all evaluators that reference this employee
            var evaluatorIds = await _context.Evaluators
                .Where(e => e.EmployeeId == user.EmployeeId)
                .Select(e => e.Id)
                .ToListAsync();

            if (!evaluatorIds.Any())
            {
                return null;
            }

            // Get submission with all related data
            var submission = await _context.SurveySubmissions
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(ss => ss.Evaluator)
                    .ThenInclude(e => e.Employee)
                .Include(ss => ss.Survey)
                .Include(ss => ss.SubjectEvaluatorSurvey)
                    .ThenInclude(ses => ses.SubjectEvaluator)
                .FirstOrDefaultAsync(ss => ss.Id == submissionId && evaluatorIds.Contains(ss.EvaluatorId));

            if (submission == null)
            {
                _logger.LogWarning("Submission {SubmissionId} not found for employee {EmployeeId}", submissionId, user.EmployeeId);
                return null;
            }

            var subjectEmployee = submission.Subject.Employee;
            var evaluatorEmployee = submission.Evaluator.Employee;

            return new SubmissionDetailResponse
            {
                SubmissionId = submission.Id,
                AssignmentId = submission.SubjectEvaluatorSurveyId,
                SurveyId = submission.SurveyId,
                SurveyTitle = submission.Survey.Title,
                SurveyDescription = submission.Survey.Description ?? "",
                SurveySchema = submission.Survey.Schema,
                ResponseData = submission.ResponseData ?? JsonDocument.Parse("{}"),
                SubjectName = $"{subjectEmployee.FirstName} {subjectEmployee.LastName}",
                EvaluatorName = $"{evaluatorEmployee.FirstName} {evaluatorEmployee.LastName}",
                IsSelfEvaluation = submission.Survey.IsSelfEvaluation,
                RelationshipType = submission.SubjectEvaluatorSurvey.SubjectEvaluator.Relationship,
                CompletedAt = submission.CompletedAt ?? DateTime.UtcNow,
                SubmittedAt = submission.CompletedAt ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission detail for submission {SubmissionId}", submissionId);
            throw;
        }
    }

    public async Task<List<AssignedEvaluationResponse>> GetEvaluatorFormsAsync(Guid evaluatorId)
    {
        try
        {
            _logger.LogInformation("Getting forms for evaluator {EvaluatorId}", evaluatorId);

            // Get all survey assignments where this evaluator is assigned
            var assignments = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                .Include(ses => ses.Survey)
                .Where(ses => ses.SubjectEvaluator.EvaluatorId == evaluatorId && ses.IsActive)
                .ToListAsync();

            _logger.LogInformation("Found {Count} survey assignments for evaluator {EvaluatorId}", assignments.Count, evaluatorId);

            // Get submissions for these assignments
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await _context.SurveySubmissions
                .Where(ss => assignmentIds.Contains(ss.SubjectEvaluatorSurveyId))
                .ToListAsync();

            // Build response
            var result = assignments.Select(a =>
            {
                var submission = submissions.FirstOrDefault(s => s.SubjectEvaluatorSurveyId == a.Id);
                var subjectEmployee = a.SubjectEvaluator.Subject.Employee;

                return new AssignedEvaluationResponse
                {
                    AssignmentId = a.Id,
                    SurveyId = a.SurveyId,
                    SubjectId = a.SubjectEvaluator.SubjectId,
                    EvaluatorId = a.SubjectEvaluator.EvaluatorId,
                    SubjectName = $"{subjectEmployee.FirstName} {subjectEmployee.LastName}",
                    SubjectEmail = subjectEmployee.Email,
                    SubjectDepartment = subjectEmployee.Department ?? "",
                    SubjectDesignation = subjectEmployee.Designation ?? "",
                    SurveyTitle = a.Survey.Title,
                    SurveyDescription = a.Survey.Description ?? "",
                    IsSelfEvaluation = a.Survey.IsSelfEvaluation,
                    RelationshipType = a.SubjectEvaluator.Relationship,
                    Status = submission?.Status ?? SurveySubmissionStatus.Pending,
                    StartedAt = submission?.StartedAt,
                    CompletedAt = submission?.CompletedAt,
                    AssignedAt = a.CreatedAt,
                    DueDate = null
                };
            }).ToList();

            return result.OrderByDescending(r => r.AssignedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forms for evaluator {EvaluatorId}", evaluatorId);
            throw;
        }
    }

    /// <summary>
    /// Replace placeholders in survey schema with actual names
    /// </summary>
    private JsonDocument ReplacePlaceholders(JsonDocument schema, string subjectName, string evaluatorName)
    {
        try
        {
            var jsonString = schema.RootElement.GetRawText();
            jsonString = jsonString.Replace("{subjectName}", subjectName);
            jsonString = jsonString.Replace("{evaluatorName}", evaluatorName);
            return JsonDocument.Parse(jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing placeholders in survey schema");
            return schema;
        }
    }
}
