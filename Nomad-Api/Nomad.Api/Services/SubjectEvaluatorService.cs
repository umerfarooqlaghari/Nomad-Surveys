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

    public SubjectEvaluatorService(
        NomadSurveysDbContext context, 
        IMapper mapper, 
        ILogger<SubjectEvaluatorService> logger,
        ISubjectService subjectService,
        IEvaluatorService evaluatorService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _subjectService = subjectService;
        _evaluatorService = evaluatorService;
    }

    public async Task<AssignmentResponse> AssignEvaluatorsToSubjectAsync(Guid subjectId, AssignEvaluatorsToSubjectRequest request)
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
                    .FirstOrDefaultAsync(e => e.Id == evaluatorRequest.EvaluatorId && 
                                            e.TenantId == subject.TenantId && 
                                            e.IsActive);

                if (evaluator == null)
                {
                    errors.Add($"Evaluator {evaluatorRequest.EvaluatorId} not found or not in same tenant");
                    continue;
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
    }

    public async Task<AssignmentResponse> AssignSubjectsToEvaluatorAsync(Guid evaluatorId, AssignSubjectsToEvaluatorRequest request)
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
                    .FirstOrDefaultAsync(s => s.Id == subjectRequest.SubjectId && 
                                            s.TenantId == evaluator.TenantId && 
                                            s.IsActive);

                if (subject == null)
                {
                    errors.Add($"Subject {subjectRequest.SubjectId} not found or not in same tenant");
                    continue;
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
                .Include(se => se.Evaluator)
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
                    FirstName = se.Subject.FirstName,
                    LastName = se.Subject.LastName,
                    FullName = se.Subject.FullName,
                    Email = se.Subject.Email,

                    Designation = se.Subject.Designation,
                    IsActive = se.Subject.IsActive
                } : null,
                Evaluator = se.Evaluator != null ? new EvaluatorSummaryResponse
                {
                    Id = se.Evaluator.Id,
                    FirstName = se.Evaluator.FirstName,
                    LastName = se.Evaluator.LastName,
                    FullName = se.Evaluator.FullName,
                    EvaluatorEmail = se.Evaluator.EvaluatorEmail,

                    Designation = se.Evaluator.Designation,
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
                .Include(se => se.Evaluator)
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
                    FirstName = se.Subject.FirstName,
                    LastName = se.Subject.LastName,
                    FullName = se.Subject.FullName,
                    Email = se.Subject.Email,

                    Designation = se.Subject.Designation,
                    IsActive = se.Subject.IsActive
                } : null,
                Evaluator = se.Evaluator != null ? new EvaluatorSummaryResponse
                {
                    Id = se.Evaluator.Id,
                    FirstName = se.Evaluator.FirstName,
                    LastName = se.Evaluator.LastName,
                    FullName = se.Evaluator.FullName,
                    EvaluatorEmail = se.Evaluator.EvaluatorEmail,

                    Designation = se.Evaluator.Designation,
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
            .Include(se => se.Evaluator)
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
                FirstName = assignment.Subject.FirstName,
                LastName = assignment.Subject.LastName,
                FullName = assignment.Subject.FullName,
                Email = assignment.Subject.Email,

                Designation = assignment.Subject.Designation,
                IsActive = assignment.Subject.IsActive
            } : null,
            Evaluator = assignment.Evaluator != null ? new EvaluatorSummaryResponse
            {
                Id = assignment.Evaluator.Id,
                FirstName = assignment.Evaluator.FirstName,
                LastName = assignment.Evaluator.LastName,
                FullName = assignment.Evaluator.FullName,
                EvaluatorEmail = assignment.Evaluator.EvaluatorEmail,

                Designation = assignment.Evaluator.Designation,
                IsActive = assignment.Evaluator.IsActive
            } : null
        };
    }
}
