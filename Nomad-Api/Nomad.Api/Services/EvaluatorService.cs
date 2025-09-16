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
    private const string DefaultPassword = "Password@123";

    public EvaluatorService(NomadSurveysDbContext context, IMapper mapper, ILogger<EvaluatorService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
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
                .Include(e => e.SubjectEvaluators)
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            return evaluators.Select(e => new EvaluatorListResponse
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FullName,
                EvaluatorEmail = e.EvaluatorEmail,

                CompanyName = e.CompanyName,
                Designation = e.Designation,
                Location = e.Location,
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
                .Include(e => e.Tenant)
                .Include(e => e.SubjectEvaluators)
                    .ThenInclude(se => se.Subject)
                .FirstOrDefaultAsync(e => e.Id == evaluatorId && e.IsActive);

            if (evaluator == null)
                return null;

            var response = _mapper.Map<EvaluatorResponse>(evaluator);
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
                    Subject = se.Subject != null ? new SubjectSummaryResponse
                    {
                        Id = se.Subject.Id,
                        FirstName = se.Subject.FirstName,
                        LastName = se.Subject.LastName,
                        FullName = se.Subject.FullName,
                        Email = se.Subject.Email,

                        Designation = se.Subject.Designation,
                        IsActive = se.Subject.IsActive
                    } : null
                }).ToList();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluator {EvaluatorId}", evaluatorId);
            throw;
        }
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
            var evaluatorsToCreate = new List<Evaluator>();
            var errors = new List<string>();

            // Validate all evaluators first
            for (int i = 0; i < request.Evaluators.Count; i++)
            {
                var evaluatorRequest = request.Evaluators[i];
                var validationErrors = await ValidateEvaluatorAsync(evaluatorRequest, tenantId, null);
                
                if (validationErrors.Any())
                {
                    errors.AddRange(validationErrors.Select(e => $"Evaluator {i + 1}: {e}"));
                    continue;
                }

                var evaluator = _mapper.Map<Evaluator>(evaluatorRequest);
                evaluator.Id = Guid.NewGuid();
                evaluator.TenantId = tenantId;
                evaluator.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
                evaluator.CreatedAt = DateTime.UtcNow;
                evaluator.IsActive = true;

                evaluatorsToCreate.Add(evaluator);
            }

            // Bulk insert valid evaluators
            if (evaluatorsToCreate.Any())
            {
                await _context.Evaluators.AddRangeAsync(evaluatorsToCreate);
                await _context.SaveChangesAsync();
                
                response.SuccessfullyCreated = evaluatorsToCreate.Count;
                response.CreatedIds = evaluatorsToCreate.Select(e => e.Id).ToList();
            }

            response.Failed = response.TotalRequested - response.SuccessfullyCreated;
            response.Errors = errors;

            await transaction.CommitAsync();
            
            _logger.LogInformation("Bulk created {Count} evaluators for tenant {TenantId}", 
                response.SuccessfullyCreated, tenantId);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error bulk creating evaluators for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<EvaluatorResponse?> UpdateEvaluatorAsync(Guid evaluatorId, UpdateEvaluatorRequest request)
    {
        try
        {
            var evaluator = await _context.Evaluators
                .Include(e => e.Tenant)
                .FirstOrDefaultAsync(e => e.Id == evaluatorId && e.IsActive);

            if (evaluator == null)
                return null;

            // Validate the update
            var validationErrors = await ValidateEvaluatorAsync(request, evaluator.TenantId, evaluatorId);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            // Update properties
            _mapper.Map(request, evaluator);
            evaluator.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated evaluator {EvaluatorId}", evaluatorId);

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
                .FirstOrDefaultAsync(e => e.Id == evaluatorId && e.IsActive);

            if (evaluator == null)
                return false;

            // Soft delete
            evaluator.IsActive = false;
            evaluator.UpdatedAt = DateTime.UtcNow;

            // Also soft delete related assignments
            var assignments = await _context.SubjectEvaluators
                .Where(se => se.EvaluatorId == evaluatorId && se.IsActive)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
                assignment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted evaluator {EvaluatorId}", evaluatorId);

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
            .Where(e => e.EvaluatorEmail == email && e.TenantId == tenantId && e.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private async Task<List<string>> ValidateEvaluatorAsync(dynamic evaluatorRequest, Guid tenantId, Guid? excludeId = null)
    {
        var errors = new List<string>();



        // Check email uniqueness within tenant
        if (await EvaluatorExistsByEmailAsync(evaluatorRequest.EvaluatorEmail, tenantId, excludeId))
        {
            errors.Add($"Evaluator with email '{evaluatorRequest.EvaluatorEmail}' already exists in this tenant");
        }

        return errors;
    }
}
