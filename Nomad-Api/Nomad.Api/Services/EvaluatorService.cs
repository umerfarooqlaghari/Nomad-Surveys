using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using BCrypt.Net;

namespace Nomad.Api.Services;

public class EvaluatorService : IEvaluatorService
{
    private readonly NomadSurveysDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<EvaluatorService> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IRelationshipService _relationshipService;
    private readonly IPasswordGenerator _passwordGenerator;

    public EvaluatorService(
        NomadSurveysDbContext context,
        IMapper mapper,
        ILogger<EvaluatorService> logger,
        IAuthenticationService authenticationService,
        IRelationshipService relationshipService,
        IPasswordGenerator passwordGenerator)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _authenticationService = authenticationService;
        _relationshipService = relationshipService;
        _passwordGenerator = passwordGenerator;
    }

    public async Task<List<EvaluatorListResponse>> GetEvaluatorsAsync(Guid? tenantId = null)
    {
        try
        {
            var query = _context.Evaluators
                .Include(e => e.Employee)
                .Include(e => e.SubjectEvaluators)
                .Where(e => e.IsActive && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            var evaluators = await query
                .OrderBy(e => e.Employee.FirstName)
                .ThenBy(e => e.Employee.LastName)
                .ToListAsync();

            var evaluatorIds = evaluators.Select(e => e.Id).ToList();
            var employeeIds = evaluators.Select(e => e.EmployeeId).ToList();

            // Get subject records for cross-role check
            var subjectRecords = await _context.Subjects
                .Where(s => employeeIds.Contains(s.EmployeeId) && s.IsActive && (!tenantId.HasValue || s.TenantId == tenantId.Value))
                .ToDictionaryAsync(s => s.EmployeeId);

            // Get completed evaluation counts (assignments for these evaluators)
            var completedCountDict = await _context.SurveySubmissions
                .Where(ss => evaluatorIds.Contains(ss.EvaluatorId) && ss.Status == SurveySubmissionStatus.Completed)
                .GroupBy(ss => ss.EvaluatorId)
                .Select(g => new { EvaluatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EvaluatorId, x => x.Count);

            var totalCountDict = await _context.SubjectEvaluatorSurveys
                .Where(ses => evaluatorIds.Contains(ses.SubjectEvaluator.EvaluatorId) && ses.IsActive)
                .GroupBy(ses => ses.SubjectEvaluator.EvaluatorId)
                .Select(g => new { EvaluatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EvaluatorId, x => x.Count);

            // Get received evaluation counts (if they are subjects)
            var subjectIdsForTheseEvaluators = subjectRecords.Values.Select(s => s.Id).ToList();

            var completedReceivedCounts = await _context.SurveySubmissions
                .Where(ss => subjectIdsForTheseEvaluators.Contains(ss.SubjectId) && ss.Status == SurveySubmissionStatus.Completed)
                .GroupBy(ss => ss.SubjectId)
                .Select(g => new { SubjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SubjectId, x => x.Count);

            var totalReceivedCounts = await _context.SubjectEvaluatorSurveys
                .Where(ses => subjectIdsForTheseEvaluators.Contains(ses.SubjectEvaluator.SubjectId) && ses.IsActive)
                .GroupBy(ses => ses.SubjectEvaluator.SubjectId)
                .Select(g => new { SubjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SubjectId, x => x.Count);

            return evaluators.Select(e =>
            {
                var isSubject = subjectRecords.ContainsKey(e.EmployeeId);
                var subjectId = isSubject ? (Guid?)subjectRecords[e.EmployeeId].Id : null;

                var totalCompleted = totalCountDict.GetValueOrDefault(e.Id, 0);
                var completedCount = completedCountDict.GetValueOrDefault(e.Id, 0);

                var totalReceived = subjectId.HasValue ? totalReceivedCounts.GetValueOrDefault(subjectId.Value, 0) : 0;
                var completedReceived = subjectId.HasValue ? completedReceivedCounts.GetValueOrDefault(subjectId.Value, 0) : 0;

                return new EvaluatorListResponse
                {
                    Id = e.Id,
                    EmployeeId = e.EmployeeId,
                    FirstName = e.Employee.FirstName,
                    LastName = e.Employee.LastName,
                    FullName = e.Employee.FullName,
                    Email = e.Employee.Email,
                    EvaluatorEmail = e.Employee.Email,
                    EmployeeIdString = e.Employee.EmployeeId,
                    CompanyName = e.Employee.CompanyName,
                    Designation = e.Employee.Designation,
                    Department = e.Employee.Department,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt,
                    LastLoginAt = e.LastLoginAt,
                    TenantId = e.TenantId,
                    SubjectCount = e.SubjectEvaluators.Count(se => se.IsActive),
                    EvaluationsCompleted = $"{completedCount}/{totalCompleted}",
                    EvaluationsReceived = isSubject ? $"{completedReceived}/{totalReceived}" : "-",
                    IsSubject = isSubject
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluators for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<EvaluatorResponse?> GetEvaluatorByIdAsync(Guid evaluatorId)
    {
        try
        {
            var evaluator = await _context.Evaluators
                .Include(e => e.Employee)
                .Include(e => e.Tenant)
                .Include(e => e.SubjectEvaluators)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .FirstOrDefaultAsync(e => e.Id == evaluatorId && e.IsActive);

            if (evaluator == null)
                return null;

            var response = new EvaluatorResponse
            {
                Id = evaluator.Id,
                EmployeeId = evaluator.EmployeeId,
                Employee = _mapper.Map<EmployeeResponse>(evaluator.Employee),
                IsActive = evaluator.IsActive,
                CreatedAt = evaluator.CreatedAt,
                UpdatedAt = evaluator.UpdatedAt,
                LastLoginAt = evaluator.LastLoginAt,
                TenantId = evaluator.TenantId,
                Tenant = _mapper.Map<TenantResponse>(evaluator.Tenant)
            };

            response.Subjects = evaluator.SubjectEvaluators
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
                    Subject = new SubjectSummaryResponse
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
                    }
                }).ToList();

            response.AssignedSubjectIds = response.Subjects.Select(s => s.SubjectId).ToList();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluator {EvaluatorId}", evaluatorId);
            throw;
        }
    }

    public async Task<EvaluatorResponse?> UpdateEvaluatorAsync(Guid evaluatorId, UpdateEvaluatorRequest request)
    {
        try
        {
            var evaluator = await _context.Evaluators
                .FirstOrDefaultAsync(e => e.Id == evaluatorId && e.IsActive);

            if (evaluator == null)
            {
                return null;
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.TenantId == evaluator.TenantId && e.IsActive);

            if (employee == null)
            {
                throw new InvalidOperationException($"Employee with EmployeeId '{request.EmployeeId}' not found");
            }

            evaluator.EmployeeId = employee.Id;
            evaluator.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetEvaluatorByIdAsync(evaluatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating evaluator {EvaluatorId}", evaluatorId);
            throw;
        }
    }

    public async Task<bool> DeleteEvaluatorAsync(Guid evaluatorId)
    {
        try
        {
            var evaluator = await _context.Evaluators
                .Include(e => e.SubjectEvaluators)
                .FirstOrDefaultAsync(e => e.Id == evaluatorId);

            if (evaluator == null)
                return false;

            evaluator.IsActive = false;
            evaluator.UpdatedAt = DateTime.UtcNow;

            foreach (var relationship in evaluator.SubjectEvaluators)
            {
                relationship.IsActive = false;
                relationship.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting evaluator {EvaluatorId}", evaluatorId);
            throw;
        }
    }

    public async Task<bool> EvaluatorExistsAsync(Guid evaluatorId, Guid? tenantId = null)
    {
        var query = _context.Evaluators.Where(e => e.Id == evaluatorId && e.IsActive);

        if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> EvaluatorExistsByEmailAsync(string email, Guid tenantId, Guid? excludeId = null)
    {
        var query = _context.Evaluators
            .Include(e => e.Employee)
            .Where(e => e.Employee.Email == email && e.TenantId == tenantId && e.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<BulkCreateResponse> BulkCreateEvaluatorsAsync(BulkCreateEvaluatorsRequest request, Guid tenantId)
    {
        var response = new BulkCreateResponse
        {
            TotalRequested = request.Evaluators.Count
        };

        return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var evaluatorRequest in request.Evaluators)
                {
                    try
                    {
                        var employee = await _context.Employees
                            .FirstOrDefaultAsync(e => e.EmployeeId == evaluatorRequest.EmployeeId && e.TenantId == tenantId && e.IsActive);

                        if (employee == null)
                        {
                            response.Failed++;
                            response.Errors.Add($"Employee '{evaluatorRequest.EmployeeId}' not found");
                            continue;
                        }

                        var existingEvaluator = await _context.Evaluators
                            .FirstOrDefaultAsync(e => e.EmployeeId == employee.Id && e.TenantId == tenantId);

                        if (existingEvaluator != null)
                        {
                            if (!existingEvaluator.IsActive)
                            {
                                existingEvaluator.IsActive = true;
                                existingEvaluator.UpdatedAt = DateTime.UtcNow;
                                _context.Evaluators.Update(existingEvaluator);
                                await _context.SaveChangesAsync();

                                response.UpdatedCount++;
                                response.CreatedIds.Add(existingEvaluator.Id);

                                _logger.LogInformation("✅ Reactivated evaluator for employee {EmployeeId}", evaluatorRequest.EmployeeId);

                                // Handle relationships when reactivating
                                if (evaluatorRequest.SubjectRelationships != null && evaluatorRequest.SubjectRelationships.Any())
                                {
                                    var subjectRelationships = evaluatorRequest.SubjectRelationships
                                        .Select(sr => (sr.SubjectEmployeeId, sr.Relationship))
                                        .ToList();

                                    await _relationshipService.MergeEvaluatorSubjectRelationshipsWithTypesAsync(
                                        existingEvaluator.Id,
                                        subjectRelationships,
                                        tenantId
                                    );
                                }
                            }
                            else
                            {
                                response.UpdatedCount++;
                                response.CreatedIds.Add(existingEvaluator.Id);

                                _logger.LogInformation("ℹ️ Evaluator already active for employee {EmployeeId}", evaluatorRequest.EmployeeId);

                                if (evaluatorRequest.SubjectRelationships != null && evaluatorRequest.SubjectRelationships.Any())
                                {
                                    var subjectRelationships = evaluatorRequest.SubjectRelationships
                                        .Select(sr => (sr.SubjectEmployeeId, sr.Relationship))
                                        .ToList();

                                    await _relationshipService.MergeEvaluatorSubjectRelationshipsWithTypesAsync(
                                        existingEvaluator.Id,
                                        subjectRelationships,
                                        tenantId
                                    );
                                }
                            }
                        }
                        else
                        {
                            var newEvaluator = new Evaluator
                            {
                                Id = Guid.NewGuid(),
                                EmployeeId = employee.Id,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(_passwordGenerator.Generate(employee.Email)),
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                TenantId = tenantId
                            };

                            _context.Evaluators.Add(newEvaluator);
                            await _context.SaveChangesAsync();

                            response.SuccessfullyCreated++;
                            response.CreatedIds.Add(newEvaluator.Id);

                            if (evaluatorRequest.SubjectRelationships != null && evaluatorRequest.SubjectRelationships.Any())
                            {
                                var subjectRelationships = evaluatorRequest.SubjectRelationships
                                    .Select(sr => (sr.SubjectEmployeeId, sr.Relationship))
                                    .ToList();
                                
                                await _relationshipService.CreateEvaluatorSubjectRelationshipsWithTypesAsync(
                                    newEvaluator.Id,
                                    subjectRelationships,
                                    tenantId
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing evaluator {EmployeeId}", evaluatorRequest.EmployeeId);
                        response.Failed++;
                        response.Errors.Add($"Error processing '{evaluatorRequest.EmployeeId}': {ex.Message}");
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk create evaluators");
                throw;
            }

            return response;
        });
    }
}
