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
    private const string DefaultPassword = "Password@123";

    public EvaluatorService(
        NomadSurveysDbContext context,
        IMapper mapper,
        ILogger<EvaluatorService> logger,
        IAuthenticationService authenticationService,
        IRelationshipService relationshipService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _authenticationService = authenticationService;
        _relationshipService = relationshipService;
    }

    public async Task<List<EvaluatorListResponse>> GetEvaluatorsAsync(Guid? tenantId = null)
    {
        try
        {
            var query = _context.Evaluators.AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(e => e.TenantId == tenantId.Value);
            }

            var evaluators = await query
                .Include(e => e.Employee)
                .Include(e => e.SubjectEvaluators)
                .Where(e => e.IsActive)
                .OrderBy(e => e.Employee.FirstName)
                .ThenBy(e => e.Employee.LastName)
                .ToListAsync();

            return evaluators.Select(e => new EvaluatorListResponse
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
                SubjectCount = e.SubjectEvaluators.Count(se => se.IsActive)
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
                            response.UpdatedCount++;
                            response.CreatedIds.Add(existingEvaluator.Id);
                        }
                        else
                        {
                            response.UpdatedCount++;
                            response.CreatedIds.Add(existingEvaluator.Id);

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
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
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
    }
}
