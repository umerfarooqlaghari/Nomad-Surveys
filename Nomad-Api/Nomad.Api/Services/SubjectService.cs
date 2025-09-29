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
            var query = _context.Subjects.AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(s => s.TenantId == tenantId.Value);
            }

            var subjects = await query
                .Include(s => s.SubjectEvaluators)
                .Where(s => s.IsActive)
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();

            return subjects.Select(s => new SubjectListResponse
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                FullName = s.FullName,
                Email = s.Email,
                EmployeeId = s.EmployeeId,
                CompanyName = s.CompanyName,
                Designation = s.Designation,
                Location = s.Location,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                LastLoginAt = s.LastLoginAt,
                TenantId = s.TenantId,
                EvaluatorCount = s.SubjectEvaluators.Count(se => se.IsActive)
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
                .Include(s => s.Tenant)
                .Include(s => s.SubjectEvaluators)
                    .ThenInclude(se => se.Evaluator)
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.IsActive);

            if (subject == null)
                return null;

            var response = _mapper.Map<SubjectResponse>(subject);
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

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject {SubjectId}", subjectId);
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
            var subjectsToCreate = new List<Subject>();
            var subjectsToUpdate = new List<(Subject ExistingSubject, CreateSubjectRequest UpdateRequest)>();
            var errors = new List<string>();

            _logger.LogInformation("Processing {Count} subjects for bulk create/update", request.Subjects.Count);

            // Get all existing subjects by EmployeeId in this tenant
            var requestedEmployeeIds = request.Subjects.Select(s => s.EmployeeId).ToList();
            var existingSubjects = await _context.Subjects
                .Where(s => requestedEmployeeIds.Contains(s.EmployeeId) && s.TenantId == tenantId)
                .ToListAsync();

            var existingEmployeeIds = existingSubjects.Select(s => s.EmployeeId).ToHashSet();

            _logger.LogInformation("Found {ExistingCount} existing subjects out of {RequestedCount} requested",
                existingSubjects.Count, request.Subjects.Count);

            // Separate new subjects from updates
            for (int i = 0; i < request.Subjects.Count; i++)
            {
                var subjectRequest = request.Subjects[i];
                var validationErrors = await ValidateSubjectAsync(subjectRequest, tenantId, null);

                if (validationErrors.Any())
                {
                    errors.AddRange(validationErrors.Select(e => $"Subject {i + 1}: {e}"));
                    continue;
                }

                if (existingEmployeeIds.Contains(subjectRequest.EmployeeId))
                {
                    // This is an update
                    var existingSubject = existingSubjects.First(s => s.EmployeeId == subjectRequest.EmployeeId);
                    subjectsToUpdate.Add((existingSubject, subjectRequest));
                    _logger.LogInformation("Subject {EmployeeId} exists - will update", subjectRequest.EmployeeId);
                }
                else
                {
                    // This is a new subject
                    var subject = _mapper.Map<Subject>(subjectRequest);
                    subject.Id = Guid.NewGuid();
                    subject.TenantId = tenantId;
                    subject.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
                    subject.CreatedAt = DateTime.UtcNow;
                    subject.IsActive = true;

                    subjectsToCreate.Add(subject);
                    _logger.LogInformation("Subject {EmployeeId} is new - will create", subjectRequest.EmployeeId);
                }
            }

            // Update existing subjects
            foreach (var (existingSubject, updateRequest) in subjectsToUpdate)
            {
                _logger.LogInformation("Updating existing subject {EmployeeId} (ID: {SubjectId})",
                    existingSubject.EmployeeId, existingSubject.Id);

                // Update basic fields
                existingSubject.FirstName = updateRequest.FirstName;
                existingSubject.LastName = updateRequest.LastName;
                existingSubject.Email = updateRequest.Email;
                existingSubject.CompanyName = updateRequest.CompanyName;
                existingSubject.Gender = updateRequest.Gender;
                existingSubject.BusinessUnit = updateRequest.BusinessUnit;
                existingSubject.Grade = updateRequest.Grade;
                existingSubject.Designation = updateRequest.Designation;
                existingSubject.Tenure = updateRequest.Tenure;
                existingSubject.Location = updateRequest.Location;
                existingSubject.Metadata1 = updateRequest.Metadata1;
                existingSubject.Metadata2 = updateRequest.Metadata2;
                existingSubject.UpdatedAt = DateTime.UtcNow;

                _context.Subjects.Update(existingSubject);
                response.UpdatedCount++;

                // Handle relationship merging for updated subjects
                try
                {
                    if (updateRequest.EvaluatorRelationships != null && updateRequest.EvaluatorRelationships.Any())
                    {
                        _logger.LogInformation("Merging enhanced relationships for existing subject {SubjectId}", existingSubject.Id);

                        var evaluatorRelationships = updateRequest.EvaluatorRelationships
                            .Select(er => (er.EvaluatorId, er.Relationship))
                            .ToList();

                        var relationshipResult = await _relationshipService.MergeSubjectEvaluatorRelationshipsWithTypesAsync(
                            existingSubject.Id, evaluatorRelationships, tenantId);

                        _logger.LogInformation("Enhanced relationship merge result for subject {SubjectId}: {SuccessfulConnections} successful, {FailedCount} failed",
                            existingSubject.Id, relationshipResult.SuccessfulConnections, relationshipResult.FailedEmployeeIds.Count);

                        if (relationshipResult.Warnings.Any())
                        {
                            errors.AddRange(relationshipResult.Warnings.Select(w => $"Subject {existingSubject.EmployeeId}: {w}"));
                        }
                    }
                    else if (updateRequest.RelatedEmployeeIds != null && updateRequest.RelatedEmployeeIds.Any())
                    {
                        _logger.LogInformation("Merging simple relationships for existing subject {SubjectId}", existingSubject.Id);

                        var relationshipResult = await _relationshipService.MergeSubjectEvaluatorRelationshipsAsync(
                            existingSubject.Id, updateRequest.RelatedEmployeeIds, tenantId);

                        _logger.LogInformation("Simple relationship merge result for subject {SubjectId}: {SuccessfulConnections} successful, {FailedCount} failed",
                            existingSubject.Id, relationshipResult.SuccessfulConnections, relationshipResult.FailedEmployeeIds.Count);

                        if (relationshipResult.Warnings.Any())
                        {
                            errors.AddRange(relationshipResult.Warnings.Select(w => $"Subject {existingSubject.EmployeeId}: {w}"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to merge relationships for existing subject {SubjectId}", existingSubject.Id);
                    errors.Add($"Subject {existingSubject.EmployeeId}: Failed to merge relationships - {ex.Message}");
                }
            }

            // Create new subjects and ApplicationUser accounts
            if (subjectsToCreate.Count > 0)
            {
                var createdUserIds = new List<Guid>();
                var createdSubjectIds = new List<Guid>();

                foreach (var subject in subjectsToCreate)
                {
                    try
                    {
                        // Create ApplicationUser account
                        var createUserRequest = new CreateUserRequest
                        {
                            FirstName = subject.FirstName,
                            LastName = subject.LastName,
                            Email = subject.Email,
                            Password = DefaultPassword,
                            Roles = new List<string> { "Participant" }
                        };

                        var userResponse = await _authenticationService.CreateUserAsync(createUserRequest, tenantId);
                        createdUserIds.Add(userResponse.Id);

                        // Link the subject to the user
                        subject.UserId = userResponse.Id;

                        await _context.Subjects.AddAsync(subject);
                        createdSubjectIds.Add(subject.Id);

                        _logger.LogInformation("Created subject {SubjectId} with user account {UserId}",
                            subject.Id, userResponse.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create subject with email {Email}", subject.Email);
                        errors.Add($"Failed to create subject {subject.Email}: {ex.Message}");
                    }
                }

                if (createdSubjectIds.Count > 0)
                {
                    await _context.SaveChangesAsync();

                    // Create relationships after subjects are saved
                    var relationshipWarnings = new List<string>();
                    for (int i = 0; i < subjectsToCreate.Count; i++)
                    {
                        var subject = subjectsToCreate[i];
                        var originalRequest = request.Subjects.FirstOrDefault(r => r.EmployeeId == subject.EmployeeId);

                        _logger.LogInformation("Processing relationships for subject {EmployeeId} (ID: {SubjectId})",
                            subject.EmployeeId, subject.Id);

                        if (originalRequest == null)
                        {
                            _logger.LogWarning("Original request not found for subject {EmployeeId}", subject.EmployeeId);
                            continue;
                        }

                        _logger.LogInformation("Original request found - EvaluatorRelationships: {HasEnhanced}, RelatedEmployeeIds: {HasSimple}",
                            originalRequest.EvaluatorRelationships?.Any() ?? false,
                            originalRequest.RelatedEmployeeIds?.Any() ?? false);

                        // Handle enhanced relationships with types
                        if (originalRequest?.EvaluatorRelationships != null && originalRequest.EvaluatorRelationships.Any())
                        {
                            _logger.LogInformation("Creating enhanced relationships for subject {SubjectId} with {Count} evaluators",
                                subject.Id, originalRequest.EvaluatorRelationships.Count);

                            try
                            {
                                var evaluatorRelationships = originalRequest.EvaluatorRelationships
                                    .Select(er => (er.EvaluatorId, er.Relationship))
                                    .ToList();

                                var relationshipResult = await _relationshipService.CreateSubjectEvaluatorRelationshipsWithTypesAsync(
                                    subject.Id, evaluatorRelationships, tenantId);

                                _logger.LogInformation("Enhanced relationship creation result for subject {SubjectId}: {SuccessfulConnections} successful, {FailedCount} failed",
                                    subject.Id, relationshipResult.SuccessfulConnections, relationshipResult.FailedEmployeeIds.Count);

                                if (relationshipResult.Warnings.Any())
                                {
                                    relationshipWarnings.AddRange(relationshipResult.Warnings.Select(w => $"Subject {subject.EmployeeId}: {w}"));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to create enhanced relationships for subject {SubjectId}", subject.Id);
                                relationshipWarnings.Add($"Subject {subject.EmployeeId}: Failed to create enhanced relationships - {ex.Message}");
                            }
                        }
                        // Fallback to simple relationships for backward compatibility
                        else if (originalRequest?.RelatedEmployeeIds != null && originalRequest.RelatedEmployeeIds.Any())
                        {
                            _logger.LogInformation("Creating simple relationships for subject {SubjectId} with evaluators: {EvaluatorIds}",
                                subject.Id, string.Join(", ", originalRequest.RelatedEmployeeIds));

                            try
                            {
                                var relationshipResult = await _relationshipService.CreateSubjectEvaluatorRelationshipsAsync(
                                    subject.Id, originalRequest.RelatedEmployeeIds, tenantId);

                                _logger.LogInformation("Simple relationship creation result for subject {SubjectId}: {SuccessfulConnections} successful, {FailedCount} failed",
                                    subject.Id, relationshipResult.SuccessfulConnections, relationshipResult.FailedEmployeeIds.Count);

                                if (relationshipResult.Warnings.Any())
                                {
                                    relationshipWarnings.AddRange(relationshipResult.Warnings.Select(w => $"Subject {subject.EmployeeId}: {w}"));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to create simple relationships for subject {SubjectId}", subject.Id);
                                relationshipWarnings.Add($"Subject {subject.EmployeeId}: Failed to create simple relationships - {ex.Message}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No relationship data found for subject {SubjectId}", subject.Id);
                        }
                    }

                    if (relationshipWarnings.Any())
                    {
                        errors.AddRange(relationshipWarnings);
                    }
                }

                response.SuccessfullyCreated = createdSubjectIds.Count;
                response.CreatedIds = createdSubjectIds;
            }

            // Calculate totals including both created and updated
            var totalProcessed = response.SuccessfullyCreated + response.UpdatedCount;
            response.Failed = response.TotalRequested - totalProcessed;
            response.Errors = errors;

            await transaction.CommitAsync();

            _logger.LogInformation("Bulk processed {TotalProcessed} subjects for tenant {TenantId}: {Created} created, {Updated} updated",
                totalProcessed, tenantId, response.SuccessfullyCreated, response.UpdatedCount);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error bulk creating subjects for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<SubjectResponse?> UpdateSubjectAsync(Guid subjectId, UpdateSubjectRequest request)
    {
        try
        {
            var subject = await _context.Subjects
                .Include(s => s.Tenant)
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.IsActive);

            if (subject == null)
                return null;

            // Validate the update
            var validationErrors = await ValidateSubjectAsync(request, subject.TenantId, subjectId);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            // Update properties
            _mapper.Map(request, subject);
            subject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Handle relationship updates if provided
            if (request.EvaluatorRelationships != null)
            {
                try
                {
                    // Remove existing relationships
                    var existingRelationships = await _context.SubjectEvaluators
                        .Where(se => se.SubjectId == subjectId)
                        .ToListAsync();

                    _context.SubjectEvaluators.RemoveRange(existingRelationships);
                    await _context.SaveChangesAsync();

                    // Create new relationships with types
                    if (request.EvaluatorRelationships.Any())
                    {
                        var evaluatorRelationships = request.EvaluatorRelationships
                            .Select(er => (er.EvaluatorId, er.Relationship))
                            .ToList();

                        await _relationshipService.CreateSubjectEvaluatorRelationshipsWithTypesAsync(
                            subjectId, evaluatorRelationships, subject.TenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update relationships for subject {SubjectId}", subjectId);
                    // Don't throw here, just log the error as the subject update was successful
                }
            }
            else if (request.RelatedEmployeeIds != null)
            {
                try
                {
                    // Remove existing relationships
                    var existingRelationships = await _context.SubjectEvaluators
                        .Where(se => se.SubjectId == subjectId)
                        .ToListAsync();

                    _context.SubjectEvaluators.RemoveRange(existingRelationships);
                    await _context.SaveChangesAsync();

                    // Create new relationships (simple)
                    if (request.RelatedEmployeeIds.Any())
                    {
                        await _relationshipService.CreateSubjectEvaluatorRelationshipsAsync(
                            subjectId, request.RelatedEmployeeIds, subject.TenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update relationships for subject {SubjectId}", subjectId);
                    // Don't throw here, just log the error as the subject update was successful
                }
            }


            _logger.LogInformation("Updated subject {SubjectId}", subjectId);

            return await GetSubjectByIdAsync(subjectId);
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
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.IsActive);

            if (subject == null)
                return false;

            // Soft delete
            subject.IsActive = false;
            subject.UpdatedAt = DateTime.UtcNow;

            // Also soft delete related assignments
            var assignments = await _context.SubjectEvaluators
                .Where(se => se.SubjectId == subjectId && se.IsActive)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
                assignment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted subject {SubjectId}", subjectId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {SubjectId}", subjectId);
            throw;
        }
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
            .Where(s => s.Email == email && s.TenantId == tenantId && s.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private async Task<List<string>> ValidateSubjectAsync(dynamic subjectRequest, Guid tenantId, Guid? excludeId = null)
    {
        var errors = new List<string>();



        // Check email uniqueness within tenant
        if (await SubjectExistsByEmailAsync(subjectRequest.Email, tenantId, excludeId))
        {
            errors.Add($"Subject with email '{subjectRequest.Email}' already exists in this tenant");
        }

        return errors;
    }
}
