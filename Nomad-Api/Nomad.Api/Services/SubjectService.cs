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
    private const string DefaultPassword = "Password@123";

    public SubjectService(
        NomadSurveysDbContext context,
        IMapper mapper,
        ILogger<SubjectService> logger,
        IAuthenticationService authenticationService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _authenticationService = authenticationService;
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
            var errors = new List<string>();

            // Validate all subjects first
            for (int i = 0; i < request.Subjects.Count; i++)
            {
                var subjectRequest = request.Subjects[i];
                var validationErrors = await ValidateSubjectAsync(subjectRequest, tenantId, null);
                
                if (validationErrors.Any())
                {
                    errors.AddRange(validationErrors.Select(e => $"Subject {i + 1}: {e}"));
                    continue;
                }

                var subject = _mapper.Map<Subject>(subjectRequest);
                subject.Id = Guid.NewGuid();
                subject.TenantId = tenantId;
                subject.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
                subject.CreatedAt = DateTime.UtcNow;
                subject.IsActive = true;

                subjectsToCreate.Add(subject);
            }

            // Create ApplicationUser accounts and subjects
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
                }

                response.SuccessfullyCreated = createdSubjectIds.Count;
                response.CreatedIds = createdSubjectIds;
            }

            response.Failed = response.TotalRequested - response.SuccessfullyCreated;
            response.Errors = errors;

            await transaction.CommitAsync();
            
            _logger.LogInformation("Bulk created {Count} subjects for tenant {TenantId}", 
                response.SuccessfullyCreated, tenantId);

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
