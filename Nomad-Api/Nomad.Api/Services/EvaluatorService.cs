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
                EmployeeId = e.EmployeeId,
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
            var evaluatorsToUpdate = new List<(Evaluator ExistingEvaluator, CreateEvaluatorRequest UpdateRequest)>();
            var errors = new List<string>();

            _logger.LogInformation("Processing {Count} evaluators for bulk create/update", request.Evaluators.Count);

            // Get all existing evaluators by EmployeeId in this tenant
            var requestedEmployeeIds = request.Evaluators.Select(e => e.EmployeeId).ToList();
            var existingEvaluators = await _context.Evaluators
                .Where(e => requestedEmployeeIds.Contains(e.EmployeeId) && e.TenantId == tenantId)
                .ToListAsync();

            var existingEmployeeIds = existingEvaluators.Select(e => e.EmployeeId).ToHashSet();

            _logger.LogInformation("Found {ExistingCount} existing evaluators out of {RequestedCount} requested",
                existingEvaluators.Count, request.Evaluators.Count);

            // Separate new evaluators from updates
            for (int i = 0; i < request.Evaluators.Count; i++)
            {
                var evaluatorRequest = request.Evaluators[i];
                var validationErrors = await ValidateEvaluatorAsync(evaluatorRequest, tenantId, null);

                if (validationErrors.Any())
                {
                    errors.AddRange(validationErrors.Select(e => $"Evaluator {i + 1}: {e}"));
                    continue;
                }

                if (existingEmployeeIds.Contains(evaluatorRequest.EmployeeId))
                {
                    // This is an update
                    var existingEvaluator = existingEvaluators.First(e => e.EmployeeId == evaluatorRequest.EmployeeId);
                    evaluatorsToUpdate.Add((existingEvaluator, evaluatorRequest));
                    _logger.LogInformation("Evaluator {EmployeeId} exists - will update", evaluatorRequest.EmployeeId);
                }
                else
                {
                    // This is a new evaluator
                    var evaluator = _mapper.Map<Evaluator>(evaluatorRequest);
                    evaluator.Id = Guid.NewGuid();
                    evaluator.TenantId = tenantId;
                    evaluator.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
                    evaluator.CreatedAt = DateTime.UtcNow;
                    evaluator.IsActive = true;

                    evaluatorsToCreate.Add(evaluator);
                    _logger.LogInformation("Evaluator {EmployeeId} is new - will create", evaluatorRequest.EmployeeId);
                }
            }

            // Update existing evaluators
            foreach (var (existingEvaluator, updateRequest) in evaluatorsToUpdate)
            {
                _logger.LogInformation("Updating existing evaluator {EmployeeId} (ID: {EvaluatorId})",
                    existingEvaluator.EmployeeId, existingEvaluator.Id);

                // Update basic fields
                existingEvaluator.FirstName = updateRequest.FirstName;
                existingEvaluator.LastName = updateRequest.LastName;
                existingEvaluator.EvaluatorEmail = updateRequest.EvaluatorEmail;
                existingEvaluator.CompanyName = updateRequest.CompanyName;
                existingEvaluator.Gender = updateRequest.Gender;
                existingEvaluator.BusinessUnit = updateRequest.BusinessUnit;
                existingEvaluator.Grade = updateRequest.Grade;
                existingEvaluator.Designation = updateRequest.Designation;
                existingEvaluator.Tenure = updateRequest.Tenure;
                existingEvaluator.Location = updateRequest.Location;
                existingEvaluator.Metadata1 = updateRequest.Metadata1;
                existingEvaluator.Metadata2 = updateRequest.Metadata2;
                existingEvaluator.UpdatedAt = DateTime.UtcNow;

                _context.Evaluators.Update(existingEvaluator);
                response.UpdatedCount++;

                // Handle relationship merging for updated evaluators
                try
                {
                    if (updateRequest.SubjectRelationships != null && updateRequest.SubjectRelationships.Any())
                    {
                        _logger.LogInformation("Merging enhanced relationships for existing evaluator {EvaluatorId}", existingEvaluator.Id);

                        var subjectRelationships = updateRequest.SubjectRelationships
                            .Select(sr => (sr.SubjectId, sr.Relationship))
                            .ToList();

                        var relationshipResult = await _relationshipService.MergeEvaluatorSubjectRelationshipsWithTypesAsync(
                            existingEvaluator.Id, subjectRelationships, tenantId);

                        _logger.LogInformation("Enhanced relationship merge result for evaluator {EvaluatorId}: {SuccessfulConnections} successful, {FailedCount} failed",
                            existingEvaluator.Id, relationshipResult.SuccessfulConnections, relationshipResult.FailedEmployeeIds.Count);

                        if (relationshipResult.Warnings.Any())
                        {
                            errors.AddRange(relationshipResult.Warnings.Select(w => $"Evaluator {existingEvaluator.EmployeeId}: {w}"));
                        }
                    }
                    else if (updateRequest.RelatedEmployeeIds != null && updateRequest.RelatedEmployeeIds.Any())
                    {
                        _logger.LogInformation("Merging simple relationships for existing evaluator {EvaluatorId}", existingEvaluator.Id);

                        var relationshipResult = await _relationshipService.MergeEvaluatorSubjectRelationshipsAsync(
                            existingEvaluator.Id, updateRequest.RelatedEmployeeIds, tenantId);

                        _logger.LogInformation("Simple relationship merge result for evaluator {EvaluatorId}: {SuccessfulConnections} successful, {FailedCount} failed",
                            existingEvaluator.Id, relationshipResult.SuccessfulConnections, relationshipResult.FailedEmployeeIds.Count);

                        if (relationshipResult.Warnings.Any())
                        {
                            errors.AddRange(relationshipResult.Warnings.Select(w => $"Evaluator {existingEvaluator.EmployeeId}: {w}"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to merge relationships for existing evaluator {EvaluatorId}", existingEvaluator.Id);
                    errors.Add($"Evaluator {existingEvaluator.EmployeeId}: Failed to merge relationships - {ex.Message}");
                }
            }

            // Create new evaluators and ApplicationUser accounts
            if (evaluatorsToCreate.Count > 0)
            {
                var createdUserIds = new List<Guid>();
                var createdEvaluatorIds = new List<Guid>();

                foreach (var evaluator in evaluatorsToCreate)
                {
                    try
                    {
                        // Create ApplicationUser account
                        var createUserRequest = new CreateUserRequest
                        {
                            FirstName = evaluator.FirstName,
                            LastName = evaluator.LastName,
                            Email = evaluator.EvaluatorEmail,
                            Password = DefaultPassword,
                            Roles = new List<string> { "Participant" }
                        };

                        var userResponse = await _authenticationService.CreateUserAsync(createUserRequest, tenantId);
                        createdUserIds.Add(userResponse.Id);

                        // Link the evaluator to the user
                        evaluator.UserId = userResponse.Id;

                        await _context.Evaluators.AddAsync(evaluator);
                        createdEvaluatorIds.Add(evaluator.Id);

                        _logger.LogInformation("Created evaluator {EvaluatorId} with user account {UserId}",
                            evaluator.Id, userResponse.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create evaluator with email {Email}", evaluator.EvaluatorEmail);
                        errors.Add($"Failed to create evaluator {evaluator.EvaluatorEmail}: {ex.Message}");
                    }
                }

                if (createdEvaluatorIds.Count > 0)
                {
                    await _context.SaveChangesAsync();

                    // Create relationships after evaluators are saved
                    var relationshipWarnings = new List<string>();
                    for (int i = 0; i < evaluatorsToCreate.Count; i++)
                    {
                        var evaluator = evaluatorsToCreate[i];
                        var originalRequest = request.Evaluators.FirstOrDefault(r => r.EmployeeId == evaluator.EmployeeId);

                        if (originalRequest?.RelatedEmployeeIds != null && originalRequest.RelatedEmployeeIds.Any())
                        {
                            try
                            {
                                var relationshipResult = await _relationshipService.CreateEvaluatorSubjectRelationshipsAsync(
                                    evaluator.Id, originalRequest.RelatedEmployeeIds, tenantId);

                                if (relationshipResult.Warnings.Any())
                                {
                                    relationshipWarnings.AddRange(relationshipResult.Warnings.Select(w => $"Evaluator {evaluator.EmployeeId}: {w}"));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to create relationships for evaluator {EvaluatorId}", evaluator.Id);
                                relationshipWarnings.Add($"Evaluator {evaluator.EmployeeId}: Failed to create relationships - {ex.Message}");
                            }
                        }
                    }

                    if (relationshipWarnings.Any())
                    {
                        errors.AddRange(relationshipWarnings);
                    }
                }

                response.SuccessfullyCreated = createdEvaluatorIds.Count;
                response.CreatedIds = createdEvaluatorIds;
            }

            // Calculate totals including both created and updated
            var totalProcessed = response.SuccessfullyCreated + response.UpdatedCount;
            response.Failed = response.TotalRequested - totalProcessed;
            response.Errors = errors;

            await transaction.CommitAsync();

            _logger.LogInformation("Bulk processed {TotalProcessed} evaluators for tenant {TenantId}: {Created} created, {Updated} updated",
                totalProcessed, tenantId, response.SuccessfullyCreated, response.UpdatedCount);

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

            // Handle relationship updates if provided
            if (request.RelatedEmployeeIds != null)
            {
                try
                {
                    // Remove existing relationships
                    var existingRelationships = await _context.SubjectEvaluators
                        .Where(se => se.EvaluatorId == evaluatorId)
                        .ToListAsync();

                    _context.SubjectEvaluators.RemoveRange(existingRelationships);
                    await _context.SaveChangesAsync();

                    // Create new relationships
                    if (request.RelatedEmployeeIds.Any())
                    {
                        await _relationshipService.CreateEvaluatorSubjectRelationshipsAsync(
                            evaluatorId, request.RelatedEmployeeIds, evaluator.TenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update relationships for evaluator {EvaluatorId}", evaluatorId);
                    // Don't throw here, just log the error as the evaluator update was successful
                }
            }

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
