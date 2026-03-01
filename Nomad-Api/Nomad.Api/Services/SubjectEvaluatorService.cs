using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class SubjectEvaluatorService : ISubjectEvaluatorService
{
    private readonly NomadSurveysDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SubjectEvaluatorService> _logger;
    private readonly ISubjectService _subjectService;
    private readonly IEvaluatorService _evaluatorService;
    private readonly IPasswordGenerator _passwordGenerator;

    public SubjectEvaluatorService(
        NomadSurveysDbContext context,
        IMapper mapper,
        ILogger<SubjectEvaluatorService> logger,
        ISubjectService subjectService,
        IEvaluatorService evaluatorService,
        IPasswordGenerator passwordGenerator)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _subjectService = subjectService;
        _evaluatorService = evaluatorService;
        _passwordGenerator = passwordGenerator;
    }

    public async Task<AssignmentResponse> AssignEvaluatorsToSubjectAsync(Guid subjectId, AssignEvaluatorsToSubjectRequest request)
    {
        return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify subject exists
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.Id == subjectId && s.IsActive);

                if (subject == null)
                {
                    return new AssignmentResponse
                    {
                        Success = false,
                        Message = "Subject not found"
                    };
                }

                var assignments = new List<SubjectEvaluator>();
                var errors = new List<string>();

                foreach (var evaluatorRequest in request.Evaluators)
                {
                    // Verify evaluator exists and belongs to same tenant
                    var evaluator = await _context.Evaluators
                        .Include(e => e.Employee)
                        .FirstOrDefaultAsync(e => e.Id == evaluatorRequest.EvaluatorId &&
                                                e.TenantId == subject.TenantId &&
                                                e.IsActive);

                    if (evaluator == null)
                    {
                        // Try to find the employee by ID to auto-create evaluator
                        var employee = await _context.Employees
                            .FirstOrDefaultAsync(e => e.Id == evaluatorRequest.EvaluatorId &&
                                                    e.TenantId == subject.TenantId &&
                                                    e.IsActive);

                        if (employee != null)
                        {
                            // Auto-create evaluator record
                            evaluator = new Evaluator
                            {
                                Id = Guid.NewGuid(),
                                EmployeeId = employee.Id,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(_passwordGenerator.Generate(employee.Email)),
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                TenantId = subject.TenantId
                            };

                            _context.Evaluators.Add(evaluator);
                            await _context.SaveChangesAsync();

                            // Reload with Employee navigation property
                            evaluator = await _context.Evaluators
                                .Include(e => e.Employee)
                                .FirstOrDefaultAsync(e => e.Id == evaluator.Id);

                            _logger.LogInformation("Auto-created evaluator for employee {EmployeeId} ({EmployeeName})",
                                employee.EmployeeId, employee.FullName);
                        }
                        else
                        {
                            errors.Add($"Employee {evaluatorRequest.EvaluatorId} not found or not in same tenant");
                            continue;
                        }
                    }

                    // Validate self-evaluation: if relationship is "Self", subject and evaluator must reference the same employee
                    if (string.Equals(evaluatorRequest.Relationship, "Self", StringComparison.OrdinalIgnoreCase))
                    {
                        var subjectWithEmployee = await _context.Subjects
                            .Include(s => s.Employee)
                            .FirstOrDefaultAsync(s => s.Id == subjectId);

                        if (subjectWithEmployee?.EmployeeId != evaluator.EmployeeId)
                        {
                            errors.Add($"Relationship type 'Self' requires subject and evaluator to reference the same employee. Subject EmployeeId: {subjectWithEmployee?.EmployeeId}, Evaluator EmployeeId: {evaluator.EmployeeId}");
                            continue;
                        }
                    }

                    // Check if assignment already exists
                    var existingAssignment = await _context.SubjectEvaluators
                        .FirstOrDefaultAsync(se => se.SubjectId == subjectId && 
                                                 se.EvaluatorId == evaluatorRequest.EvaluatorId);

                    if (existingAssignment != null)
                    {
                        if (existingAssignment.IsActive)
                        {
                            errors.Add($"Assignment between subject and evaluator {evaluatorRequest.EvaluatorId} already exists");
                            continue;
                        }
                        else
                        {
                            // Reactivate existing assignment
                            existingAssignment.IsActive = true;
                            existingAssignment.Relationship = evaluatorRequest.Relationship;
                            existingAssignment.UpdatedAt = DateTime.UtcNow;
                            assignments.Add(existingAssignment);
                        }
                    }
                    else
                    {
                        // Create new assignment
                        var assignment = new SubjectEvaluator
                        {
                            Id = Guid.NewGuid(),
                            SubjectId = subjectId,
                            EvaluatorId = evaluatorRequest.EvaluatorId,
                            Relationship = evaluatorRequest.Relationship,
                            TenantId = subject.TenantId,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        await _context.SubjectEvaluators.AddAsync(assignment);
                        assignments.Add(assignment);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var assignmentResponses = new List<SubjectEvaluatorResponse>();
                foreach (var assignment in assignments)
                {
                    var assignmentResponse = await GetSubjectEvaluatorResponseAsync(assignment.Id);
                    if (assignmentResponse != null)
                    {
                        assignmentResponses.Add(assignmentResponse);
                    }
                }

                _logger.LogInformation("Assigned {Count} evaluators to subject {SubjectId}", 
                    assignments.Count, subjectId);

                return new AssignmentResponse
                {
                    Success = true,
                    Message = $"Successfully assigned {assignments.Count} evaluators. {errors.Count} errors occurred.",
                    Assignments = assignmentResponses
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error assigning evaluators to subject {SubjectId}", subjectId);
                throw;
            }
        });
    }

    public async Task<AssignmentResponse> AssignSubjectsToEvaluatorAsync(Guid evaluatorId, AssignSubjectsToEvaluatorRequest request)
    {
        return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify evaluator exists
                var evaluator = await _context.Evaluators
                    .FirstOrDefaultAsync(e => e.Id == evaluatorId && e.IsActive);

                if (evaluator == null)
                {
                    return new AssignmentResponse
                    {
                        Success = false,
                        Message = "Evaluator not found"
                    };
                }

                var assignments = new List<SubjectEvaluator>();
                var errors = new List<string>();

                foreach (var subjectRequest in request.Subjects)
                {
                    // Verify subject exists and belongs to same tenant
                    var subject = await _context.Subjects
                        .Include(s => s.Employee)
                        .FirstOrDefaultAsync(s => s.Id == subjectRequest.SubjectId &&
                                                s.TenantId == evaluator.TenantId &&
                                                s.IsActive);

                    if (subject == null)
                    {
                        // Try to find the employee by ID to auto-create subject
                        var employee = await _context.Employees
                            .FirstOrDefaultAsync(e => e.Id == subjectRequest.SubjectId &&
                                                    e.TenantId == evaluator.TenantId &&
                                                    e.IsActive);

                        if (employee != null)
                        {
                            // Auto-create subject record
                            subject = new Subject
                            {
                                Id = Guid.NewGuid(),
                                EmployeeId = employee.Id,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(_passwordGenerator.Generate(employee.Email)),
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                TenantId = evaluator.TenantId
                            };

                            _context.Subjects.Add(subject);
                            await _context.SaveChangesAsync();

                            // Reload with Employee navigation property
                            subject = await _context.Subjects
                                .Include(s => s.Employee)
                                .FirstOrDefaultAsync(s => s.Id == subject.Id);

                            _logger.LogInformation("Auto-created subject for employee {EmployeeId} ({EmployeeName})",
                                employee.EmployeeId, employee.FullName);
                        }
                        else
                        {
                            errors.Add($"Employee {subjectRequest.SubjectId} not found or not in same tenant");
                            continue;
                        }
                    }

                    // Validate self-evaluation: if relationship is "Self", subject and evaluator must reference the same employee
                    if (string.Equals(subjectRequest.Relationship, "Self", StringComparison.OrdinalIgnoreCase))
                    {
                        var evaluatorWithEmployee = await _context.Evaluators
                            .Include(e => e.Employee)
                            .FirstOrDefaultAsync(e => e.Id == evaluatorId);

                        if (subject.EmployeeId != evaluatorWithEmployee?.EmployeeId)
                        {
                            errors.Add($"Relationship type 'Self' requires subject and evaluator to reference the same employee. Subject EmployeeId: {subject.EmployeeId}, Evaluator EmployeeId: {evaluatorWithEmployee?.EmployeeId}");
                            continue;
                        }
                    }

                    // Check if assignment already exists
                    var existingAssignment = await _context.SubjectEvaluators
                        .FirstOrDefaultAsync(se => se.SubjectId == subjectRequest.SubjectId && 
                                                 se.EvaluatorId == evaluatorId);

                    if (existingAssignment != null)
                    {
                        if (existingAssignment.IsActive)
                        {
                            errors.Add($"Assignment between evaluator and subject {subjectRequest.SubjectId} already exists");
                            continue;
                        }
                        else
                        {
                            // Reactivate existing assignment
                            existingAssignment.IsActive = true;
                            existingAssignment.Relationship = subjectRequest.Relationship;
                            existingAssignment.UpdatedAt = DateTime.UtcNow;
                            assignments.Add(existingAssignment);
                        }
                    }
                    else
                    {
                        // Create new assignment
                        var assignment = new SubjectEvaluator
                        {
                            Id = Guid.NewGuid(),
                            SubjectId = subjectRequest.SubjectId,
                            EvaluatorId = evaluatorId,
                            Relationship = subjectRequest.Relationship,
                            TenantId = evaluator.TenantId,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        await _context.SubjectEvaluators.AddAsync(assignment);
                        assignments.Add(assignment);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var assignmentResponses = new List<SubjectEvaluatorResponse>();
                foreach (var assignment in assignments)
                {
                    var assignmentResponse = await GetSubjectEvaluatorResponseAsync(assignment.Id);
                    if (assignmentResponse != null)
                    {
                        assignmentResponses.Add(assignmentResponse);
                    }
                }

                _logger.LogInformation("Assigned {Count} subjects to evaluator {EvaluatorId}", 
                    assignments.Count, evaluatorId);

                return new AssignmentResponse
                {
                    Success = true,
                    Message = $"Successfully assigned {assignments.Count} subjects. {errors.Count} errors occurred.",
                    Assignments = assignmentResponses
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error assigning subjects to evaluator {EvaluatorId}", evaluatorId);
                throw;
            }
        });
    }

    public async Task<SubjectEvaluatorResponse?> UpdateAssignmentAsync(Guid subjectId, Guid evaluatorId, string relationship)
    {
        try
        {
            var assignment = await _context.SubjectEvaluators
                .FirstOrDefaultAsync(se => se.SubjectId == subjectId &&
                                         se.EvaluatorId == evaluatorId &&
                                         se.IsActive);

            if (assignment == null)
            {
                _logger.LogWarning("Assignment not found for subject {SubjectId} and evaluator {EvaluatorId}",
                    subjectId, evaluatorId);
                return null;
            }

            assignment.Relationship = relationship;
            assignment.UpdatedAt = DateTime.UtcNow;

            _context.SubjectEvaluators.Update(assignment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated assignment {AssignmentId} relationship to {Relationship}",
                assignment.Id, relationship);

            return await GetSubjectEvaluatorResponseAsync(assignment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment for subject {SubjectId} and evaluator {EvaluatorId}",
                subjectId, evaluatorId);
            throw;
        }
    }

    public async Task<bool> RemoveAssignmentAsync(Guid subjectId, Guid evaluatorId)
    {
        try
        {
            var assignment = await _context.SubjectEvaluators
                .FirstOrDefaultAsync(se => se.SubjectId == subjectId && 
                                         se.EvaluatorId == evaluatorId && 
                                         se.IsActive);

            if (assignment == null)
                return false;

            // Soft delete
            assignment.IsActive = false;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed assignment between subject {SubjectId} and evaluator {EvaluatorId}", 
                subjectId, evaluatorId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing assignment between subject {SubjectId} and evaluator {EvaluatorId}", 
                subjectId, evaluatorId);
            throw;
        }
    }

    public async Task<List<SubjectEvaluatorResponse>> GetSubjectEvaluatorsAsync(Guid subjectId)
    {
        try
        {
            var assignments = await _context.SubjectEvaluators
                .Include(se => se.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(se => se.Evaluator)
                    .ThenInclude(e => e.Employee)
                .Where(se => se.SubjectId == subjectId && se.IsActive)
                .ToListAsync();

            return assignments.Select(se => new SubjectEvaluatorResponse
            {
                Id = se.Id,
                SubjectId = se.SubjectId,
                EvaluatorId = se.EvaluatorId,
                Relationship = se.Relationship,
                IsActive = se.IsActive,
                CreatedAt = se.CreatedAt,
                UpdatedAt = se.UpdatedAt,
                TenantId = se.TenantId,
                Subject = se.Subject != null ? new SubjectSummaryResponse
                {
                    Id = se.Subject.Id,
                    EmployeeId = se.Subject.EmployeeId,
                    FirstName = se.Subject.Employee.FirstName,
                    LastName = se.Subject.Employee.LastName,
                    FullName = se.Subject.Employee.FullName,
                    Email = se.Subject.Employee.Email,
                    EmployeeIdString = se.Subject.Employee.EmployeeId,
                    Designation = se.Subject.Employee.Designation,
                    IsActive = se.Subject.IsActive
                } : null,
                Evaluator = se.Evaluator != null ? new EvaluatorSummaryResponse
                {
                    Id = se.Evaluator.Id,
                    EmployeeId = se.Evaluator.EmployeeId,
                    FirstName = se.Evaluator.Employee.FirstName,
                    LastName = se.Evaluator.Employee.LastName,
                    FullName = se.Evaluator.Employee.FullName,
                    Email = se.Evaluator.Employee.Email,
                    EvaluatorEmail = se.Evaluator.Employee.Email,
                    EmployeeIdString = se.Evaluator.Employee.EmployeeId,
                    Designation = se.Evaluator.Employee.Designation,
                    IsActive = se.Evaluator.IsActive
                } : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluators for subject {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<List<SubjectEvaluatorResponse>> GetEvaluatorSubjectsAsync(Guid evaluatorId)
    {
        try
        {
            var assignments = await _context.SubjectEvaluators
                .Include(se => se.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(se => se.Evaluator)
                    .ThenInclude(e => e.Employee)
                .Where(se => se.EvaluatorId == evaluatorId && se.IsActive)
                .ToListAsync();

            return assignments.Select(se => new SubjectEvaluatorResponse
            {
                Id = se.Id,
                SubjectId = se.SubjectId,
                EvaluatorId = se.EvaluatorId,
                Relationship = se.Relationship,
                IsActive = se.IsActive,
                CreatedAt = se.CreatedAt,
                UpdatedAt = se.UpdatedAt,
                TenantId = se.TenantId,
                Subject = se.Subject != null ? new SubjectSummaryResponse
                {
                    Id = se.Subject.Id,
                    EmployeeId = se.Subject.EmployeeId,
                    FirstName = se.Subject.Employee.FirstName,
                    LastName = se.Subject.Employee.LastName,
                    FullName = se.Subject.Employee.FullName,
                    Email = se.Subject.Employee.Email,
                    EmployeeIdString = se.Subject.Employee.EmployeeId,
                    Designation = se.Subject.Employee.Designation,
                    IsActive = se.Subject.IsActive
                } : null,
                Evaluator = se.Evaluator != null ? new EvaluatorSummaryResponse
                {
                    Id = se.Evaluator.Id,
                    EmployeeId = se.Evaluator.EmployeeId,
                    FirstName = se.Evaluator.Employee.FirstName,
                    LastName = se.Evaluator.Employee.LastName,
                    FullName = se.Evaluator.Employee.FullName,
                    Email = se.Evaluator.Employee.Email,
                    EvaluatorEmail = se.Evaluator.Employee.Email,
                    EmployeeIdString = se.Evaluator.Employee.EmployeeId,
                    Designation = se.Evaluator.Employee.Designation,
                    IsActive = se.Evaluator.IsActive
                } : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subjects for evaluator {EvaluatorId}", evaluatorId);
            throw;
        }
    }

    public async Task<bool> AssignmentExistsAsync(Guid subjectId, Guid evaluatorId)
    {
        return await _context.SubjectEvaluators
            .AnyAsync(se => se.SubjectId == subjectId && se.EvaluatorId == evaluatorId && se.IsActive);
    }

    private async Task<SubjectEvaluatorResponse?> GetSubjectEvaluatorResponseAsync(Guid assignmentId)
    {
        var assignment = await _context.SubjectEvaluators
            .Include(se => se.Subject)
                .ThenInclude(s => s.Employee)
            .Include(se => se.Evaluator)
                .ThenInclude(e => e.Employee)
            .FirstOrDefaultAsync(se => se.Id == assignmentId);

        if (assignment == null)
            return null;

        return new SubjectEvaluatorResponse
        {
            Id = assignment.Id,
            SubjectId = assignment.SubjectId,
            EvaluatorId = assignment.EvaluatorId,
            Relationship = assignment.Relationship,
            IsActive = assignment.IsActive,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt,
            TenantId = assignment.TenantId,
            Subject = assignment.Subject != null ? new SubjectSummaryResponse
            {
                Id = assignment.Subject.Id,
                EmployeeId = assignment.Subject.EmployeeId,
                FirstName = assignment.Subject.Employee.FirstName,
                LastName = assignment.Subject.Employee.LastName,
                FullName = assignment.Subject.Employee.FullName,
                Email = assignment.Subject.Employee.Email,
                EmployeeIdString = assignment.Subject.Employee.EmployeeId,
                Designation = assignment.Subject.Employee.Designation,
                IsActive = assignment.Subject.IsActive
            } : null,
            Evaluator = assignment.Evaluator != null ? new EvaluatorSummaryResponse
            {
                Id = assignment.Evaluator.Id,
                EmployeeId = assignment.Evaluator.EmployeeId,
                FirstName = assignment.Evaluator.Employee.FirstName,
                LastName = assignment.Evaluator.Employee.LastName,
                FullName = assignment.Evaluator.Employee.FullName,
                Email = assignment.Evaluator.Employee.Email,
                EvaluatorEmail = assignment.Evaluator.Employee.Email,
                EmployeeIdString = assignment.Evaluator.Employee.EmployeeId,
                Designation = assignment.Evaluator.Employee.Designation,
                IsActive = assignment.Evaluator.IsActive
            } : null
        };
    }

    public async Task<List<RelationshipWithSurveysResponse>> GetSubjectRelationshipsWithSurveysAsync(Guid subjectId)
    {
        try
        {
            var relationships = await _context.SubjectEvaluators
                .Include(se => se.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(se => se.Evaluator)
                    .ThenInclude(e => e.Employee)
                .Where(se => se.SubjectId == subjectId && se.IsActive)
                .ToListAsync();

            var relationshipIds = relationships.Select(r => r.Id).ToList();
            var surveyAssignments = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.Survey)
                .Where(ses => relationshipIds.Contains(ses.SubjectEvaluatorId) && ses.IsActive)
                .ToListAsync();

            var assignmentsByRelationshipId = surveyAssignments
                .GroupBy(ses => ses.SubjectEvaluatorId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return relationships.Select(se => new RelationshipWithSurveysResponse
            {
                Id = se.Id,
                SubjectId = se.SubjectId,
                EvaluatorId = se.EvaluatorId,
                Relationship = se.Relationship,
                SubjectFullName = se.Subject.Employee.FullName,
                EvaluatorFullName = se.Evaluator.Employee.FullName,
                SurveyAssignments = assignmentsByRelationshipId.TryGetValue(se.Id, out var assignments)
                    ? assignments.Select(ses => new SurveyAssignmentInfo
                    {
                        SurveyId = ses.SurveyId,
                        SurveyTitle = ses.Survey.Title
                    }).ToList()
                    : new List<SurveyAssignmentInfo>()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject relationships with surveys for subject {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<List<RelationshipWithSurveysResponse>> GetEvaluatorRelationshipsWithSurveysAsync(Guid evaluatorId)
    {
        try
        {
            var relationships = await _context.SubjectEvaluators
                .Include(se => se.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(se => se.Evaluator)
                    .ThenInclude(e => e.Employee)
                .Where(se => se.EvaluatorId == evaluatorId && se.IsActive)
                .ToListAsync();

            var relationshipIds = relationships.Select(r => r.Id).ToList();
            var surveyAssignments = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.Survey)
                .Where(ses => relationshipIds.Contains(ses.SubjectEvaluatorId) && ses.IsActive)
                .ToListAsync();

            var assignmentsByRelationshipId = surveyAssignments
                .GroupBy(ses => ses.SubjectEvaluatorId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return relationships.Select(se => new RelationshipWithSurveysResponse
            {
                Id = se.Id,
                SubjectId = se.SubjectId,
                EvaluatorId = se.EvaluatorId,
                Relationship = se.Relationship,
                SubjectFullName = se.Subject.Employee.FullName,
                EvaluatorFullName = se.Evaluator.Employee.FullName,
                SurveyAssignments = assignmentsByRelationshipId.TryGetValue(se.Id, out var assignments)
                    ? assignments.Select(ses => new SurveyAssignmentInfo
                    {
                        SurveyId = ses.SurveyId,
                        SurveyTitle = ses.Survey.Title
                    }).ToList()
                    : new List<SurveyAssignmentInfo>()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluator relationships with surveys for evaluator {EvaluatorId}", evaluatorId);
            throw;
        }
    }
    public async Task<List<EmailingListItemResponse>> GetEmailingListAsync(Guid tenantId)
    {
        try
        {
            // Fetch only necessary fields into memory to avoid deep includes and large data transfer
            var flatAssignments = await _context.SubjectEvaluatorSurveys
                .AsNoTracking()
                .Where(ses => ses.TenantId == tenantId && ses.IsActive)
                .Select(ses => new
                {
                    ses.Id,
                    ses.SurveyId,
                    SurveyTitle = ses.Survey.Title,
                    EvaluatorId = ses.SubjectEvaluator.EvaluatorId,
                    EvaluatorName = ses.SubjectEvaluator.Evaluator.Employee.FullName,
                    EvaluatorEmail = ses.SubjectEvaluator.Evaluator.Employee.Email,
                    SubjectName = ses.SubjectEvaluator.Subject.Employee.FullName,
                    ses.LastReminderSentAt,
                    IsCompleted = _context.SurveySubmissions.Any(ss => ss.SubjectEvaluatorSurveyId == ses.Id && ss.Status == "Completed")
                })
                .ToListAsync();

            if (!flatAssignments.Any())
            {
                return new List<EmailingListItemResponse>();
            }

            // Grouping in memory AFTER fetching only the required fields
            var grouped = flatAssignments
                .GroupBy(a => new { a.SurveyId, a.EvaluatorId })
                .Select(g => new EmailingListItemResponse
                {
                    SurveyId = g.Key.SurveyId,
                    SurveyName = g.First().SurveyTitle,
                    EvaluatorId = g.Key.EvaluatorId,
                    EvaluatorName = g.First().EvaluatorName,
                    EvaluatorEmail = g.First().EvaluatorEmail,
                    SubjectCount = g.Count(),
                    CompletedCount = g.Count(a => a.IsCompleted),
                    PendingCount = g.Count(a => !a.IsCompleted),
                    SubjectNames = g.Select(a => a.SubjectName).Distinct().ToList(),
                    LastReminderSentAt = g.Max(a => a.LastReminderSentAt),
                    AssignmentEmailSentAt = null,
                    SubjectEvaluatorSurveyIds = g.Select(a => a.Id).ToList()
                })
                .OrderBy(r => r.SurveyName)
                .ThenBy(r => r.EvaluatorName)
                .ToList();

            return grouped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emailing list for tenant {TenantId}", tenantId);
            throw;
        }
    }
}
