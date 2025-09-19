using Nomad.Api.Data;
using Nomad.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nomad.Api.DTOs.Request;

namespace Nomad.Api.Services;

public interface IRelationshipService
{
    Task<RelationshipResult> CreateSubjectEvaluatorRelationshipsAsync(
        Guid subjectId,
        List<string> evaluatorEmployeeIds,
        Guid tenantId);

    Task<RelationshipResult> CreateSubjectEvaluatorRelationshipsWithTypesAsync(
        Guid subjectId,
        List<(string EmployeeId, string RelationshipType)> evaluatorRelationships,
        Guid tenantId);

    Task<RelationshipResult> CreateEvaluatorSubjectRelationshipsAsync(
        Guid evaluatorId,
        List<string> subjectEmployeeIds,
        Guid tenantId);

    Task<RelationshipResult> CreateEvaluatorSubjectRelationshipsWithTypesAsync(
        Guid evaluatorId,
        List<(string EmployeeId, string RelationshipType)> subjectRelationships,
        Guid tenantId);

    Task<RelationshipResult> MergeSubjectEvaluatorRelationshipsAsync(
        Guid subjectId,
        List<string> newEvaluatorEmployeeIds,
        Guid tenantId);

    Task<RelationshipResult> MergeSubjectEvaluatorRelationshipsWithTypesAsync(
        Guid subjectId,
        List<(string EmployeeId, string RelationshipType)> newEvaluatorRelationships,
        Guid tenantId);

    Task<RelationshipResult> MergeEvaluatorSubjectRelationshipsAsync(
        Guid evaluatorId,
        List<string> newSubjectEmployeeIds,
        Guid tenantId);

    Task<RelationshipResult> MergeEvaluatorSubjectRelationshipsWithTypesAsync(
        Guid evaluatorId,
        List<(string EmployeeId, string RelationshipType)> newSubjectRelationships,
        Guid tenantId);

    Task<List<string>> ValidateEmployeeIdsAsync(
        List<string> employeeIds,
        Guid tenantId,
        bool isEvaluator = false);

    Task<ValidationResponse> ValidateEmployeeIdsDetailedAsync(
        List<string> employeeIds,
        Guid tenantId,
        bool isEvaluator = false);
}

public class RelationshipResult
{
    public int SuccessfulConnections { get; set; }
    public List<string> FailedEmployeeIds { get; set; } = new();
    public List<string> DuplicateConnections { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class RelationshipService : IRelationshipService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<RelationshipService> _logger;

    public RelationshipService(NomadSurveysDbContext context, ILogger<RelationshipService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RelationshipResult> CreateSubjectEvaluatorRelationshipsAsync(
        Guid subjectId,
        List<string> evaluatorEmployeeIds,
        Guid tenantId)
    {
        _logger.LogInformation("Creating subject-evaluator relationships for subject {SubjectId} with evaluators: {EvaluatorIds}",
            subjectId, string.Join(", ", evaluatorEmployeeIds ?? new List<string>()));

        var result = new RelationshipResult();

        if (evaluatorEmployeeIds == null || !evaluatorEmployeeIds.Any())
        {
            _logger.LogInformation("No evaluator employee IDs provided for subject {SubjectId}", subjectId);
            return result;
        }

        // Get existing evaluators by EmployeeId within the tenant
        _logger.LogInformation("Looking for evaluators with EmployeeIds: {EmployeeIds} in tenant {TenantId}",
            string.Join(", ", evaluatorEmployeeIds), tenantId);

        var existingEvaluators = await _context.Evaluators
            .Where(e => evaluatorEmployeeIds.Contains(e.EmployeeId) && e.TenantId == tenantId)
            .Select(e => new { e.Id, e.EmployeeId })
            .ToListAsync();

        _logger.LogInformation("Found {Count} existing evaluators for tenant {TenantId}: {EvaluatorIds}",
            existingEvaluators.Count, tenantId, string.Join(", ", existingEvaluators.Select(e => e.EmployeeId)));

        if (existingEvaluators.Count == 0)
        {
            _logger.LogWarning("No evaluators found! This might be the issue. Check if evaluators exist in database with EmployeeIds: {EmployeeIds}",
                string.Join(", ", evaluatorEmployeeIds));
        }

        // Check for non-existent EmployeeIds
        var foundEmployeeIds = existingEvaluators.Select(e => e.EmployeeId).ToList();
        result.FailedEmployeeIds = evaluatorEmployeeIds.Except(foundEmployeeIds).ToList();

        if (result.FailedEmployeeIds.Any())
        {
            _logger.LogWarning("Evaluator EmployeeIds not found: {FailedIds}", string.Join(", ", result.FailedEmployeeIds));
        }

        // Get existing relationships to avoid duplicates
        var existingRelationships = await _context.SubjectEvaluators
            .Where(se => se.SubjectId == subjectId && 
                        existingEvaluators.Select(e => e.Id).Contains(se.EvaluatorId))
            .Select(se => se.EvaluatorId)
            .ToListAsync();

        // Create new relationships
        foreach (var evaluator in existingEvaluators)
        {
            if (existingRelationships.Contains(evaluator.Id))
            {
                result.DuplicateConnections.Add(evaluator.EmployeeId);
                continue;
            }

            var relationship = new SubjectEvaluator
            {
                Id = Guid.NewGuid(),
                SubjectId = subjectId,
                EvaluatorId = evaluator.Id,
                Relationship = "peer", // Default relationship type for simple format
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.SubjectEvaluators.Add(relationship);
            result.SuccessfulConnections++;

            _logger.LogInformation("Created relationship: Subject {SubjectId} -> Evaluator {EvaluatorId} ({EmployeeId})",
                subjectId, evaluator.Id, evaluator.EmployeeId);
        }

        _logger.LogInformation("Saving {Count} new relationships to database", result.SuccessfulConnections);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully saved relationships to database");

        // Add warnings for failed connections
        if (result.FailedEmployeeIds.Any())
        {
            result.Warnings.Add($"Evaluator EmployeeIds not found: {string.Join(", ", result.FailedEmployeeIds)}");
        }

        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Duplicate connections skipped: {string.Join(", ", result.DuplicateConnections)}");
        }

        return result;
    }

    public async Task<RelationshipResult> CreateSubjectEvaluatorRelationshipsWithTypesAsync(
        Guid subjectId,
        List<(string EmployeeId, string RelationshipType)> evaluatorRelationships,
        Guid tenantId)
    {
        _logger.LogInformation("Creating subject-evaluator relationships with types for subject {SubjectId} with {Count} evaluators",
            subjectId, evaluatorRelationships.Count);

        var result = new RelationshipResult();

        if (evaluatorRelationships == null || !evaluatorRelationships.Any())
        {
            _logger.LogInformation("No evaluator relationships provided for subject {SubjectId}", subjectId);
            return result;
        }

        var employeeIds = evaluatorRelationships.Select(er => er.EmployeeId).ToList();

        // Get existing evaluators by EmployeeId within the tenant
        var existingEvaluators = await _context.Evaluators
            .Where(e => employeeIds.Contains(e.EmployeeId) && e.TenantId == tenantId)
            .Select(e => new { e.Id, e.EmployeeId })
            .ToListAsync();

        _logger.LogInformation("Found {Count} existing evaluators for tenant {TenantId}: {EvaluatorIds}",
            existingEvaluators.Count, tenantId, string.Join(", ", existingEvaluators.Select(e => e.EmployeeId)));

        // Check for non-existent EmployeeIds
        var foundEmployeeIds = existingEvaluators.Select(e => e.EmployeeId).ToList();
        result.FailedEmployeeIds = employeeIds.Except(foundEmployeeIds).ToList();

        if (result.FailedEmployeeIds.Any())
        {
            _logger.LogWarning("Evaluator EmployeeIds not found: {FailedIds}", string.Join(", ", result.FailedEmployeeIds));
        }

        // Get existing relationships to avoid duplicates
        var existingRelationships = await _context.SubjectEvaluators
            .Where(se => se.SubjectId == subjectId &&
                        existingEvaluators.Select(e => e.Id).Contains(se.EvaluatorId))
            .Select(se => se.EvaluatorId)
            .ToListAsync();

        // Create new relationships with types
        foreach (var evaluatorRelationship in evaluatorRelationships)
        {
            var evaluator = existingEvaluators.FirstOrDefault(e => e.EmployeeId == evaluatorRelationship.EmployeeId);
            if (evaluator == null) continue;

            if (existingRelationships.Contains(evaluator.Id))
            {
                result.DuplicateConnections.Add(evaluator.EmployeeId);
                continue;
            }

            var relationship = new SubjectEvaluator
            {
                Id = Guid.NewGuid(),
                SubjectId = subjectId,
                EvaluatorId = evaluator.Id,
                Relationship = evaluatorRelationship.RelationshipType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.SubjectEvaluators.Add(relationship);
            result.SuccessfulConnections++;

            _logger.LogInformation("Created relationship: Subject {SubjectId} -> Evaluator {EvaluatorId} ({EmployeeId}) as {RelationshipType}",
                subjectId, evaluator.Id, evaluator.EmployeeId, evaluatorRelationship.RelationshipType);
        }

        _logger.LogInformation("Saving {Count} new relationships to database", result.SuccessfulConnections);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully saved relationships to database");

        // Add warnings for failed connections
        if (result.FailedEmployeeIds.Any())
        {
            result.Warnings.Add($"Evaluator EmployeeIds not found: {string.Join(", ", result.FailedEmployeeIds)}");
        }

        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Duplicate relationships skipped: {string.Join(", ", result.DuplicateConnections)}");
        }

        return result;
    }

    public async Task<RelationshipResult> CreateEvaluatorSubjectRelationshipsAsync(
        Guid evaluatorId, 
        List<string> subjectEmployeeIds, 
        Guid tenantId)
    {
        var result = new RelationshipResult();
        
        if (subjectEmployeeIds == null || !subjectEmployeeIds.Any())
            return result;

        // Get existing subjects by EmployeeId within the tenant
        var existingSubjects = await _context.Subjects
            .Where(s => subjectEmployeeIds.Contains(s.EmployeeId) && s.TenantId == tenantId)
            .Select(s => new { s.Id, s.EmployeeId })
            .ToListAsync();

        // Check for non-existent EmployeeIds
        var foundEmployeeIds = existingSubjects.Select(s => s.EmployeeId).ToList();
        result.FailedEmployeeIds = subjectEmployeeIds.Except(foundEmployeeIds).ToList();

        // Get existing relationships to avoid duplicates
        var existingRelationships = await _context.SubjectEvaluators
            .Where(se => se.EvaluatorId == evaluatorId && 
                        existingSubjects.Select(s => s.Id).Contains(se.SubjectId))
            .Select(se => se.SubjectId)
            .ToListAsync();

        // Create new relationships
        foreach (var subject in existingSubjects)
        {
            if (existingRelationships.Contains(subject.Id))
            {
                result.DuplicateConnections.Add(subject.EmployeeId);
                continue;
            }

            var relationship = new SubjectEvaluator
            {
                Id = Guid.NewGuid(),
                SubjectId = subject.Id,
                EvaluatorId = evaluatorId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.SubjectEvaluators.Add(relationship);
            result.SuccessfulConnections++;
        }

        await _context.SaveChangesAsync();

        // Add warnings for failed connections
        if (result.FailedEmployeeIds.Any())
        {
            result.Warnings.Add($"Subject EmployeeIds not found: {string.Join(", ", result.FailedEmployeeIds)}");
        }

        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Duplicate connections skipped: {string.Join(", ", result.DuplicateConnections)}");
        }

        return result;
    }

    public async Task<RelationshipResult> CreateEvaluatorSubjectRelationshipsWithTypesAsync(
        Guid evaluatorId,
        List<(string EmployeeId, string RelationshipType)> subjectRelationships,
        Guid tenantId)
    {
        _logger.LogInformation("Creating evaluator-subject relationships with types for evaluator {EvaluatorId} with {Count} subjects",
            evaluatorId, subjectRelationships.Count);

        var result = new RelationshipResult();

        if (subjectRelationships == null || !subjectRelationships.Any())
        {
            _logger.LogInformation("No subject relationships provided for evaluator {EvaluatorId}", evaluatorId);
            return result;
        }

        var employeeIds = subjectRelationships.Select(sr => sr.EmployeeId).ToList();

        // Get existing subjects by EmployeeId within the tenant
        var existingSubjects = await _context.Subjects
            .Where(s => employeeIds.Contains(s.EmployeeId) && s.TenantId == tenantId)
            .Select(s => new { s.Id, s.EmployeeId })
            .ToListAsync();

        _logger.LogInformation("Found {Count} existing subjects for tenant {TenantId}: {SubjectIds}",
            existingSubjects.Count, tenantId, string.Join(", ", existingSubjects.Select(s => s.EmployeeId)));

        // Check for non-existent EmployeeIds
        var foundEmployeeIds = existingSubjects.Select(s => s.EmployeeId).ToList();
        result.FailedEmployeeIds = employeeIds.Except(foundEmployeeIds).ToList();

        if (result.FailedEmployeeIds.Any())
        {
            _logger.LogWarning("Subject EmployeeIds not found: {FailedIds}", string.Join(", ", result.FailedEmployeeIds));
        }

        // Get existing relationships to avoid duplicates
        var existingRelationships = await _context.SubjectEvaluators
            .Where(se => se.EvaluatorId == evaluatorId &&
                        existingSubjects.Select(s => s.Id).Contains(se.SubjectId))
            .Select(se => se.SubjectId)
            .ToListAsync();

        // Create new relationships with types
        foreach (var subjectRelationship in subjectRelationships)
        {
            var subject = existingSubjects.FirstOrDefault(s => s.EmployeeId == subjectRelationship.EmployeeId);
            if (subject == null) continue;

            if (existingRelationships.Contains(subject.Id))
            {
                result.DuplicateConnections.Add(subject.EmployeeId);
                continue;
            }

            var relationship = new SubjectEvaluator
            {
                Id = Guid.NewGuid(),
                SubjectId = subject.Id,
                EvaluatorId = evaluatorId,
                Relationship = subjectRelationship.RelationshipType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.SubjectEvaluators.Add(relationship);
            result.SuccessfulConnections++;

            _logger.LogInformation("Created relationship: Evaluator {EvaluatorId} -> Subject {SubjectId} ({EmployeeId}) as {RelationshipType}",
                evaluatorId, subject.Id, subject.EmployeeId, subjectRelationship.RelationshipType);
        }

        _logger.LogInformation("Saving {Count} new relationships to database", result.SuccessfulConnections);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully saved relationships to database");

        // Add warnings for failed connections
        if (result.FailedEmployeeIds.Any())
        {
            result.Warnings.Add($"Subject EmployeeIds not found: {string.Join(", ", result.FailedEmployeeIds)}");
        }

        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Duplicate relationships skipped: {string.Join(", ", result.DuplicateConnections)}");
        }

        return result;
    }

    public async Task<RelationshipResult> MergeSubjectEvaluatorRelationshipsAsync(
        Guid subjectId,
        List<string> newEvaluatorEmployeeIds,
        Guid tenantId)
    {
        _logger.LogInformation("Merging subject-evaluator relationships for subject {SubjectId} with {Count} new evaluators",
            subjectId, newEvaluatorEmployeeIds.Count);

        var result = new RelationshipResult();

        if (newEvaluatorEmployeeIds == null || !newEvaluatorEmployeeIds.Any())
        {
            _logger.LogInformation("No new evaluator relationships to merge for subject {SubjectId}", subjectId);
            return result;
        }

        // Get existing relationships for this subject
        var existingRelationships = await _context.SubjectEvaluators
            .Where(se => se.SubjectId == subjectId && se.IsActive)
            .Include(se => se.Evaluator)
            .Select(se => se.Evaluator.EmployeeId)
            .ToListAsync();

        _logger.LogInformation("Subject {SubjectId} has {Count} existing relationships: {ExistingIds}",
            subjectId, existingRelationships.Count, string.Join(", ", existingRelationships));

        // Filter out evaluators that already have relationships
        var evaluatorsToAdd = newEvaluatorEmployeeIds.Except(existingRelationships).ToList();

        if (!evaluatorsToAdd.Any())
        {
            _logger.LogInformation("All evaluators already have relationships with subject {SubjectId}", subjectId);
            result.DuplicateConnections = newEvaluatorEmployeeIds.ToList();
            return result;
        }

        _logger.LogInformation("Adding {Count} new relationships for subject {SubjectId}: {NewIds}",
            evaluatorsToAdd.Count, subjectId, string.Join(", ", evaluatorsToAdd));

        // Create relationships for new evaluators only
        var createResult = await CreateSubjectEvaluatorRelationshipsAsync(subjectId, evaluatorsToAdd, tenantId);

        // Merge results
        result.SuccessfulConnections = createResult.SuccessfulConnections;
        result.FailedEmployeeIds = createResult.FailedEmployeeIds;
        result.DuplicateConnections = newEvaluatorEmployeeIds.Except(evaluatorsToAdd).ToList();
        result.Warnings = createResult.Warnings;

        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Skipped existing relationships: {string.Join(", ", result.DuplicateConnections)}");
        }

        return result;
    }

    public async Task<RelationshipResult> MergeSubjectEvaluatorRelationshipsWithTypesAsync(
        Guid subjectId,
        List<(string EmployeeId, string RelationshipType)> newEvaluatorRelationships,
        Guid tenantId)
    {
        _logger.LogInformation("Merging subject-evaluator relationships with types for subject {SubjectId} with {Count} new evaluators",
            subjectId, newEvaluatorRelationships.Count);

        var result = new RelationshipResult();

        if (newEvaluatorRelationships == null || !newEvaluatorRelationships.Any())
        {
            _logger.LogInformation("No new evaluator relationships to merge for subject {SubjectId}", subjectId);
            return result;
        }

        // Get existing relationships for this subject
        var existingRelationships = await _context.SubjectEvaluators
            .Where(se => se.SubjectId == subjectId && se.IsActive)
            .Include(se => se.Evaluator)
            .Select(se => se.Evaluator.EmployeeId)
            .ToListAsync();

        _logger.LogInformation("Subject {SubjectId} has {Count} existing relationships: {ExistingIds}",
            subjectId, existingRelationships.Count, string.Join(", ", existingRelationships));

        // Filter out evaluators that already have relationships
        var evaluatorsToAdd = newEvaluatorRelationships
            .Where(er => !existingRelationships.Contains(er.EmployeeId))
            .ToList();

        if (!evaluatorsToAdd.Any())
        {
            _logger.LogInformation("All evaluators already have relationships with subject {SubjectId}", subjectId);
            result.DuplicateConnections = newEvaluatorRelationships.Select(er => er.EmployeeId).ToList();
            return result;
        }

        _logger.LogInformation("Adding {Count} new relationships with types for subject {SubjectId}: {NewIds}",
            evaluatorsToAdd.Count, subjectId, string.Join(", ", evaluatorsToAdd.Select(er => $"{er.EmployeeId}({er.RelationshipType})")));

        // Create relationships for new evaluators only
        var createResult = await CreateSubjectEvaluatorRelationshipsWithTypesAsync(subjectId, evaluatorsToAdd, tenantId);

        // Merge results
        result.SuccessfulConnections = createResult.SuccessfulConnections;
        result.FailedEmployeeIds = createResult.FailedEmployeeIds;
        result.DuplicateConnections = newEvaluatorRelationships
            .Where(er => !evaluatorsToAdd.Any(eta => eta.EmployeeId == er.EmployeeId))
            .Select(er => er.EmployeeId)
            .ToList();
        result.Warnings = createResult.Warnings;

        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Skipped existing relationships: {string.Join(", ", result.DuplicateConnections)}");
        }

        return result;
    }

    public async Task<RelationshipResult> MergeEvaluatorSubjectRelationshipsAsync(
        Guid evaluatorId,
        List<string> newSubjectEmployeeIds,
        Guid tenantId)
    {
        _logger.LogInformation("Merging evaluator-subject relationships for evaluator {EvaluatorId} with {Count} new subjects",
            evaluatorId, newSubjectEmployeeIds.Count);

        var result = new RelationshipResult();

        if (newSubjectEmployeeIds == null || !newSubjectEmployeeIds.Any())
        {
            _logger.LogInformation("No new subject relationships to merge for evaluator {EvaluatorId}", evaluatorId);
            return result;
        }

        // Get existing relationships for this evaluator
        var existingRelationships = await _context.SubjectEvaluators
            .Where(se => se.EvaluatorId == evaluatorId && se.IsActive)
            .Include(se => se.Subject)
            .Select(se => se.Subject.EmployeeId)
            .ToListAsync();

        _logger.LogInformation("Evaluator {EvaluatorId} has {Count} existing relationships: {ExistingIds}",
            evaluatorId, existingRelationships.Count, string.Join(", ", existingRelationships));

        // Filter out subjects that already have relationships
        var subjectsToAdd = newSubjectEmployeeIds.Except(existingRelationships).ToList();

        if (!subjectsToAdd.Any())
        {
            _logger.LogInformation("All subjects already have relationships with evaluator {EvaluatorId}", evaluatorId);
            result.DuplicateConnections = newSubjectEmployeeIds.ToList();
            return result;
        }

        _logger.LogInformation("Adding {Count} new relationships for evaluator {EvaluatorId}: {NewIds}",
            subjectsToAdd.Count, evaluatorId, string.Join(", ", subjectsToAdd));

        // Create relationships for new subjects only
        var createResult = await CreateEvaluatorSubjectRelationshipsAsync(evaluatorId, subjectsToAdd, tenantId);

        // Merge results
        result.SuccessfulConnections = createResult.SuccessfulConnections;
        result.FailedEmployeeIds = createResult.FailedEmployeeIds;
        result.DuplicateConnections = newSubjectEmployeeIds.Except(subjectsToAdd).ToList();
        result.Warnings = createResult.Warnings;

        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Skipped existing relationships: {string.Join(", ", result.DuplicateConnections)}");
        }

        return result;
    }

    public async Task<RelationshipResult> MergeEvaluatorSubjectRelationshipsWithTypesAsync(
        Guid evaluatorId,
        List<(string EmployeeId, string RelationshipType)> newSubjectRelationships,
        Guid tenantId)
    {
        _logger.LogInformation("Merging evaluator-subject relationships with types for evaluator {EvaluatorId} with {Count} new subjects",
            evaluatorId, newSubjectRelationships.Count);

        var result = new RelationshipResult();

        if (newSubjectRelationships == null || !newSubjectRelationships.Any())
        {
            _logger.LogInformation("No new subject relationships to merge for evaluator {EvaluatorId}", evaluatorId);
            return result;
        }

        // Get existing relationships for this evaluator
        var existingRelationships = await _context.SubjectEvaluators
            .Where(se => se.EvaluatorId == evaluatorId && se.IsActive)
            .Include(se => se.Subject)
            .Select(se => se.Subject.EmployeeId)
            .ToListAsync();

        _logger.LogInformation("Evaluator {EvaluatorId} has {Count} existing relationships: {ExistingIds}",
            evaluatorId, existingRelationships.Count, string.Join(", ", existingRelationships));

        // Filter out subjects that already have relationships
        var subjectsToAdd = newSubjectRelationships
            .Where(sr => !existingRelationships.Contains(sr.EmployeeId))
            .ToList();

        if (!subjectsToAdd.Any())
        {
            _logger.LogInformation("All subjects already have relationships with evaluator {EvaluatorId}", evaluatorId);
            result.DuplicateConnections = newSubjectRelationships.Select(sr => sr.EmployeeId).ToList();
            return result;
        }

        _logger.LogInformation("Adding {Count} new relationships with types for evaluator {EvaluatorId}: {NewIds}",
            subjectsToAdd.Count, evaluatorId, string.Join(", ", subjectsToAdd.Select(sr => $"{sr.EmployeeId}({sr.RelationshipType})")));

        // Create relationships for new subjects only
        var createResult = await CreateEvaluatorSubjectRelationshipsWithTypesAsync(evaluatorId, subjectsToAdd, tenantId);

        // Merge results
        result.SuccessfulConnections = createResult.SuccessfulConnections;
        result.FailedEmployeeIds = createResult.FailedEmployeeIds;
        result.DuplicateConnections = newSubjectRelationships
            .Where(sr => !subjectsToAdd.Any(sta => sta.EmployeeId == sr.EmployeeId))
            .Select(sr => sr.EmployeeId)
            .ToList();
        result.Warnings = createResult.Warnings;

        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Skipped existing relationships: {string.Join(", ", result.DuplicateConnections)}");
        }

        return result;
    }

    public async Task<List<string>> ValidateEmployeeIdsAsync(
        List<string> employeeIds, 
        Guid tenantId, 
        bool isEvaluator = false)
    {
        if (employeeIds == null || !employeeIds.Any())
            return new List<string>();

        List<string> existingEmployeeIds;

        if (isEvaluator)
        {
            existingEmployeeIds = await _context.Evaluators
                .Where(e => employeeIds.Contains(e.EmployeeId) && e.TenantId == tenantId)
                .Select(e => e.EmployeeId)
                .ToListAsync();
        }
        else
        {
            existingEmployeeIds = await _context.Subjects
                .Where(s => employeeIds.Contains(s.EmployeeId) && s.TenantId == tenantId)
                .Select(s => s.EmployeeId)
                .ToListAsync();
        }

        return existingEmployeeIds;
    }

    public async Task<ValidationResponse> ValidateEmployeeIdsDetailedAsync(
        List<string> employeeIds,
        Guid tenantId,
        bool isEvaluator = false)
    {
        var response = new ValidationResponse
        {
            TotalRequested = employeeIds?.Count ?? 0
        };

        if (employeeIds == null || !employeeIds.Any())
        {
            return response;
        }

        if (isEvaluator)
        {
            var evaluators = await _context.Evaluators
                .Where(e => employeeIds.Contains(e.EmployeeId) && e.TenantId == tenantId)
                .Select(e => new { e.Id, e.EmployeeId, e.FirstName, e.LastName, e.EvaluatorEmail, e.IsActive })
                .ToListAsync();

            foreach (var employeeId in employeeIds)
            {
                var evaluator = evaluators.FirstOrDefault(e => e.EmployeeId == employeeId);
                if (evaluator != null)
                {
                    response.Results.Add(new ValidationResult
                    {
                        EmployeeId = employeeId,
                        IsValid = true,
                        Message = "Evaluator found",
                        Data = new
                        {
                            Id = evaluator.Id,
                            EmployeeId = evaluator.EmployeeId,
                            FullName = $"{evaluator.FirstName} {evaluator.LastName}",
                            Email = evaluator.EvaluatorEmail,
                            IsActive = evaluator.IsActive
                        }
                    });
                    response.ValidCount++;
                }
                else
                {
                    response.Results.Add(new ValidationResult
                    {
                        EmployeeId = employeeId,
                        IsValid = false,
                        Message = $"No evaluator found with EmployeeId '{employeeId}'"
                    });
                    response.InvalidCount++;
                }
            }
        }
        else
        {
            var subjects = await _context.Subjects
                .Where(s => employeeIds.Contains(s.EmployeeId) && s.TenantId == tenantId)
                .Select(s => new { s.Id, s.EmployeeId, s.FirstName, s.LastName, s.Email, s.IsActive })
                .ToListAsync();

            foreach (var employeeId in employeeIds)
            {
                var subject = subjects.FirstOrDefault(s => s.EmployeeId == employeeId);
                if (subject != null)
                {
                    response.Results.Add(new ValidationResult
                    {
                        EmployeeId = employeeId,
                        IsValid = true,
                        Message = "Subject found",
                        Data = new
                        {
                            Id = subject.Id,
                            EmployeeId = subject.EmployeeId,
                            FullName = $"{subject.FirstName} {subject.LastName}",
                            Email = subject.Email,
                            IsActive = subject.IsActive
                        }
                    });
                    response.ValidCount++;
                }
                else
                {
                    response.Results.Add(new ValidationResult
                    {
                        EmployeeId = employeeId,
                        IsValid = false,
                        Message = $"No subject found with EmployeeId '{employeeId}'"
                    });
                    response.InvalidCount++;
                }
            }
        }

        return response;
    }
}
