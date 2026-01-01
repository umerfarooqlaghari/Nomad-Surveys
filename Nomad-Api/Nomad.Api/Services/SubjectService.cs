using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using BCrypt.Net;

namespace Nomad.Api.Services;

public class SubjectService : ISubjectService
{
    private readonly NomadSurveysDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SubjectService> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IRelationshipService _relationshipService;
    private const string DefaultPassword = "Password@123";

    public SubjectService(
        NomadSurveysDbContext context,
        IMapper mapper,
        ILogger<SubjectService> logger,
        IAuthenticationService authenticationService,
        IRelationshipService relationshipService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _authenticationService = authenticationService;
        _relationshipService = relationshipService;
    }

    public async Task<List<SubjectListResponse>> GetSubjectsAsync(Guid? tenantId = null)
    {
        try
        {
            var query = _context.Subjects
                .Include(s => s.Employee)
                .Include(s => s.SubjectEvaluators)
                .Where(s => s.IsActive && (!tenantId.HasValue || s.TenantId == tenantId.Value));

            var subjects = await query
                .OrderBy(s => s.Employee.FirstName)
                .ThenBy(s => s.Employee.LastName)
                .ToListAsync();

            var subjectIds = subjects.Select(s => s.Id).ToList();
            var employeeIds = subjects.Select(s => s.EmployeeId).ToList();

            // Get evaluator records for cross-role check
            var evaluatorRecords = await _context.Evaluators
                .Where(e => employeeIds.Contains(e.EmployeeId) && e.IsActive && (!tenantId.HasValue || e.TenantId == tenantId.Value))
                .ToDictionaryAsync(e => e.EmployeeId);

            // Get received evaluation counts
            var receivedCounts = await _context.SubjectEvaluatorSurveys
                .Where(ses => subjectIds.Contains(ses.SubjectEvaluator.SubjectId) && ses.IsActive)
                .GroupBy(ses => ses.SubjectEvaluator.SubjectId)
                .Select(g => new { SubjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SubjectId, x => x.Count);

            var completedReceivedCounts = await _context.SurveySubmissions
                .Where(ss => subjectIds.Contains(ss.SubjectId) && ss.Status == SurveySubmissionStatus.Completed)
                .GroupBy(ss => ss.SubjectId)
                .Select(g => new { SubjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SubjectId, x => x.Count);

            // Get completed evaluation counts (where they are evaluators)
            var evaluatorIdsForTheseSubjects = evaluatorRecords.Values.Select(e => e.Id).ToList();
            
            var completedAsEvaluatorCounts = await _context.SurveySubmissions
                .Where(ss => evaluatorIdsForTheseSubjects.Contains(ss.EvaluatorId) && ss.Status == SurveySubmissionStatus.Completed)
                .GroupBy(ss => ss.EvaluatorId)
                .Select(g => new { EvaluatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EvaluatorId, x => x.Count);

            var totalAsEvaluatorCounts = await _context.SubjectEvaluatorSurveys
                .Where(ses => evaluatorIdsForTheseSubjects.Contains(ses.SubjectEvaluator.EvaluatorId) && ses.IsActive)
                .GroupBy(ses => ses.SubjectEvaluator.EvaluatorId)
                .Select(g => new { EvaluatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EvaluatorId, x => x.Count);

            return subjects.Select(s =>
            {
                var isEvaluator = evaluatorRecords.ContainsKey(s.EmployeeId);
                var evaluatorId = isEvaluator ? (Guid?)evaluatorRecords[s.EmployeeId].Id : null;

                var totalReceived = receivedCounts.GetValueOrDefault(s.Id, 0);
                var completedReceived = completedReceivedCounts.GetValueOrDefault(s.Id, 0);

                var totalAsEvaluator = evaluatorId.HasValue ? totalAsEvaluatorCounts.GetValueOrDefault(evaluatorId.Value, 0) : 0;
                var completedAsEvaluator = evaluatorId.HasValue ? completedAsEvaluatorCounts.GetValueOrDefault(evaluatorId.Value, 0) : 0;

                return new SubjectListResponse
                {
                    Id = s.Id,
                    EmployeeId = s.EmployeeId,
                    FirstName = s.Employee.FirstName,
                    LastName = s.Employee.LastName,
                    FullName = s.Employee.FullName,
                    Email = s.Employee.Email,
                    EmployeeIdString = s.Employee.EmployeeId,
                    CompanyName = s.Employee.CompanyName,
                    Designation = s.Employee.Designation,
                    Department = s.Employee.Department,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    LastLoginAt = s.LastLoginAt,
                    TenantId = s.TenantId,
                    EvaluatorCount = s.SubjectEvaluators.Count(se => se.IsActive),
                    EvaluationsReceived = $"{completedReceived}/{totalReceived}",
                    EvaluationsCompleted = isEvaluator ? $"{completedAsEvaluator}/{totalAsEvaluator}" : "-",
                    IsEvaluator = isEvaluator
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subjects for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<SubjectResponse?> GetSubjectByIdAsync(Guid subjectId)
    {
        try
        {
            var subject = await _context.Subjects
                .Include(s => s.Employee)
                .Include(s => s.Tenant)
                .Include(s => s.SubjectEvaluators)
                    .ThenInclude(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.IsActive);

            if (subject == null)
                return null;

            var response = new SubjectResponse
            {
                Id = subject.Id,
                EmployeeId = subject.EmployeeId,
                Employee = _mapper.Map<EmployeeResponse>(subject.Employee),
                IsActive = subject.IsActive,
                CreatedAt = subject.CreatedAt,
                UpdatedAt = subject.UpdatedAt,
                LastLoginAt = subject.LastLoginAt,
                TenantId = subject.TenantId,
                Tenant = _mapper.Map<TenantResponse>(subject.Tenant)
            };

            response.Evaluators = subject.SubjectEvaluators
                .Where(se => se.IsActive)
                .Select(se => new SubjectEvaluatorResponse
                {
                    Id = se.Id,
                    SubjectId = se.SubjectId,
                    EvaluatorId = se.EvaluatorId,
                    Relationship = se.Relationship,
                    IsActive = se.IsActive,
                    CreatedAt = se.CreatedAt,
                    UpdatedAt = se.UpdatedAt,
                    TenantId = se.TenantId,
                    Evaluator = new EvaluatorSummaryResponse
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
                    }
                }).ToList();

            response.AssignedEvaluatorIds = response.Evaluators.Select(e => e.EvaluatorId).ToList();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, Guid tenantId)
    {
        try
        {
            // Find employee by EmployeeId string (not GUID)
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.TenantId == tenantId && e.IsActive);

            if (employee == null)
            {
                throw new InvalidOperationException($"Employee with EmployeeId '{request.EmployeeId}' not found in tenant");
            }

            // Check if this employee is already a subject
            var existingSubject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id && s.TenantId == tenantId);

            if (existingSubject != null)
            {
                if (existingSubject.IsActive)
                {
                    throw new InvalidOperationException($"Employee '{request.EmployeeId}' is already a subject");
                }
                else
                {
                    // Reactivate
                    existingSubject.IsActive = true;
                    existingSubject.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return (await GetSubjectByIdAsync(existingSubject.Id))!;
                }
            }

            // Create new subject
            var subject = new Subject
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            // Handle evaluator relationships if provided
            if (request.EvaluatorRelationships != null && request.EvaluatorRelationships.Any())
            {
                var evaluatorRelationships = request.EvaluatorRelationships
                    .Select(er => (er.EvaluatorEmployeeId, er.Relationship))
                    .ToList();

                await _relationshipService.CreateSubjectEvaluatorRelationshipsWithTypesAsync(
                    subject.Id,
                    evaluatorRelationships,
                    tenantId
                );
            }

            return (await GetSubjectByIdAsync(subject.Id))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject for employee {EmployeeId}", request.EmployeeId);
            throw;
        }
    }

    public async Task<SubjectResponse?> UpdateSubjectAsync(Guid subjectId, UpdateSubjectRequest request)
    {
        try
        {
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.IsActive);

            if (subject == null)
            {
                throw new InvalidOperationException($"Subject with ID {subjectId} not found");
            }

            // Find new employee by EmployeeId string
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.TenantId == subject.TenantId && e.IsActive);

            if (employee == null)
            {
                throw new InvalidOperationException($"Employee with EmployeeId '{request.EmployeeId}' not found");
            }

            // Update employee link
            subject.EmployeeId = employee.Id;
            subject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (await GetSubjectByIdAsync(subjectId))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<bool> DeleteSubjectAsync(Guid subjectId)
    {
        try
        {
            var subject = await _context.Subjects
                .Include(s => s.SubjectEvaluators)
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null)
                return false;

            // Soft delete
            subject.IsActive = false;
            subject.UpdatedAt = DateTime.UtcNow;

            // Also soft delete all relationships
            foreach (var relationship in subject.SubjectEvaluators)
            {
                relationship.IsActive = false;
                relationship.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {SubjectId}", subjectId);
            throw;
        }
    }

    public async Task<BulkCreateResponse> BulkCreateSubjectsAsync(BulkCreateSubjectsRequest request, Guid tenantId)
    {
        var response = new BulkCreateResponse
        {
            TotalRequested = request.Subjects.Count
        };

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var subjectRequest in request.Subjects)
            {
                try
                {
                    // Find employee by EmployeeId string
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == subjectRequest.EmployeeId && e.TenantId == tenantId && e.IsActive);

                    if (employee == null)
                    {
                        response.Failed++;
                        response.Errors.Add($"Employee '{subjectRequest.EmployeeId}' not found");
                        continue;
                    }

                    // Check if already exists
                    var existingSubject = await _context.Subjects
                        .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id && s.TenantId == tenantId);

                    if (existingSubject != null)
                    {
                        if (!existingSubject.IsActive)
                        {
                            // Reactivate
                            existingSubject.IsActive = true;
                            existingSubject.UpdatedAt = DateTime.UtcNow;
                            _context.Subjects.Update(existingSubject);
                            await _context.SaveChangesAsync();

                            response.UpdatedCount++;
                            response.CreatedIds.Add(existingSubject.Id);

                            _logger.LogInformation("✅ Reactivated subject for employee {EmployeeId}", subjectRequest.EmployeeId);

                            // Handle relationships when reactivating
                            if (subjectRequest.EvaluatorRelationships != null && subjectRequest.EvaluatorRelationships.Any())
                            {
                                var evaluatorRelationships = subjectRequest.EvaluatorRelationships
                                    .Select(er => (er.EvaluatorEmployeeId, er.Relationship))
                                    .ToList();

                                await _relationshipService.MergeSubjectEvaluatorRelationshipsWithTypesAsync(
                                    existingSubject.Id,
                                    evaluatorRelationships,
                                    tenantId
                                );
                            }
                        }
                        else
                        {
                            // Already active - skip or update relationships
                            response.UpdatedCount++;
                            response.CreatedIds.Add(existingSubject.Id);

                            _logger.LogInformation("ℹ️ Subject already active for employee {EmployeeId}", subjectRequest.EmployeeId);

                            // Update relationships if provided
                            if (subjectRequest.EvaluatorRelationships != null && subjectRequest.EvaluatorRelationships.Any())
                            {
                                var evaluatorRelationships = subjectRequest.EvaluatorRelationships
                                    .Select(er => (er.EvaluatorEmployeeId, er.Relationship))
                                    .ToList();

                                await _relationshipService.MergeSubjectEvaluatorRelationshipsWithTypesAsync(
                                    existingSubject.Id,
                                    evaluatorRelationships,
                                    tenantId
                                );
                            }
                        }
                    }
                    else
                    {
                        // Create new
                        var newSubject = new Subject
                        {
                            Id = Guid.NewGuid(),
                            EmployeeId = employee.Id,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            TenantId = tenantId
                        };

                        _context.Subjects.Add(newSubject);
                        await _context.SaveChangesAsync();

                        response.SuccessfullyCreated++;
                        response.CreatedIds.Add(newSubject.Id);

                        // Handle relationships
                        if (subjectRequest.EvaluatorRelationships != null && subjectRequest.EvaluatorRelationships.Any())
                        {
                            var evaluatorRelationships = subjectRequest.EvaluatorRelationships
                                .Select(er => (er.EvaluatorEmployeeId, er.Relationship))
                                .ToList();

                            await _relationshipService.CreateSubjectEvaluatorRelationshipsWithTypesAsync(
                                newSubject.Id,
                                evaluatorRelationships,
                                tenantId
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing subject {EmployeeId}", subjectRequest.EmployeeId);
                    response.Failed++;
                    response.Errors.Add($"Error processing '{subjectRequest.EmployeeId}': {ex.Message}");
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error in bulk create subjects");
            throw;
        }

        return response;
    }

    public async Task<ValidationResponse> ValidateSubjectsAsync(List<string> employeeIds, Guid tenantId)
    {
        var response = new ValidationResponse
        {
            TotalRequested = employeeIds.Count
        };

        foreach (var employeeId in employeeIds)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.TenantId == tenantId && e.IsActive);

            if (employee == null)
            {
                response.Results.Add(new ValidationResult
                {
                    EmployeeId = employeeId,
                    IsValid = false,
                    Message = "Employee not found"
                });
                response.InvalidCount++;
            }
            else
            {
                var existingSubject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id && s.TenantId == tenantId && s.IsActive);

                if (existingSubject != null)
                {
                    response.Results.Add(new ValidationResult
                    {
                        EmployeeId = employeeId,
                        IsValid = true,
                        Message = "Already exists as subject",
                        Data = new { SubjectId = existingSubject.Id }
                    });
                    response.ValidCount++;
                }
                else
                {
                    response.Results.Add(new ValidationResult
                    {
                        EmployeeId = employeeId,
                        IsValid = true,
                        Message = "Valid - can be created",
                        Data = new { EmployeeId = employee.Id }
                    });
                    response.ValidCount++;
                }
            }
        }

        return response;
    }

    public async Task<bool> SubjectExistsAsync(Guid subjectId, Guid? tenantId = null)
    {
        var query = _context.Subjects.Where(s => s.Id == subjectId && s.IsActive);

        if (tenantId.HasValue)
        {
            query = query.Where(s => s.TenantId == tenantId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> SubjectExistsByEmailAsync(string email, Guid tenantId, Guid? excludeId = null)
    {
        var query = _context.Subjects
            .Include(s => s.Employee)
            .Where(s => s.Employee.Email == email && s.TenantId == tenantId && s.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}

