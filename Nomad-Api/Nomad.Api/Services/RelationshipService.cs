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
            .Include(e => e.Employee)
            .Where(e => evaluatorEmployeeIds.Contains(e.Employee.EmployeeId) && e.TenantId == tenantId)
            .Select(e => new { e.Id, EmployeeIdString = e.Employee.EmployeeId })
            .ToListAsync();

        _logger.LogInformation("Found {Count} existing evaluators for tenant {TenantId}: {EvaluatorIds}",
            existingEvaluators.Count, tenantId, string.Join(", ", existingEvaluators.Select(e => e.EmployeeIdString)));

        if (existingEvaluators.Count == 0)
        {
            _logger.LogWarning("No evaluators found! This might be the issue. Check if evaluators exist in database with EmployeeIds: {EmployeeIds}",
                string.Join(", ", evaluatorEmployeeIds));
        }

        // Check for non-existent EmployeeIds
        var foundEmployeeIds = existingEvaluators.Select(e => e.EmployeeIdString).ToList();
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
                result.DuplicateConnections.Add(evaluator.EmployeeIdString);
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
                subjectId, evaluator.Id, evaluator.EmployeeIdString);
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

        // Get subject's employee information for self-evaluation validation
        var subject = await _context.Subjects
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == subjectId && s.TenantId == tenantId);

        if (subject == null)
        {
            _logger.LogWarning("Subject {SubjectId} not found in tenant {TenantId}", subjectId, tenantId);
            return result;
        }

        var employeeIds = evaluatorRelationships.Select(er => er.EmployeeId).ToList();

        _logger.LogInformation("🔍 Searching for evaluators with EmployeeIds: {EmployeeIds} in tenant {TenantId}",
            string.Join(", ", employeeIds), tenantId);

        // Get ALL evaluators for this tenant first (for debugging)
        var allTenantEvaluators = await _context.Evaluators
            .Include(e => e.Employee)
            .Where(e => e.TenantId == tenantId)
            .Select(e => new { e.Id, EmployeeIdString = e.Employee.EmployeeId })
            .ToListAsync();

        _logger.LogInformation("📊 Total evaluators in tenant {TenantId}: {Count}. EmployeeIds: {AllIds}",
            tenantId, allTenantEvaluators.Count, string.Join(", ", allTenantEvaluators.Select(e => e.EmployeeIdString)));

        // Get existing evaluators by EmployeeId within the tenant (case-insensitive)
        var existingEvaluators = allTenantEvaluators
            .Where(e => employeeIds.Any(id => string.Equals(id, e.EmployeeIdString, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        _logger.LogInformation("✅ Found {Count} existing evaluators for tenant {TenantId}: {EvaluatorIds}",
            existingEvaluators.Count, tenantId, string.Join(", ", existingEvaluators.Select(e => e.EmployeeIdString)));

        // Check for non-existent EmployeeIds
        var foundEmployeeIds = existingEvaluators.Select(e => e.EmployeeIdString).ToList();
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
        _logger.LogInformation("🔄 Processing {Count} evaluator relationships", evaluatorRelationships.Count);
        foreach (var evaluatorRelationship in evaluatorRelationships)
        {
            _logger.LogInformation("🔍 Looking for evaluator with EmployeeId: {EmployeeId}", evaluatorRelationship.EmployeeId);

            var evaluator = existingEvaluators.FirstOrDefault(e =>
                string.Equals(e.EmployeeIdString, evaluatorRelationship.EmployeeId, StringComparison.OrdinalIgnoreCase));
            if (evaluator == null)
            {
                _logger.LogWarning("❌ Evaluator not found in existingEvaluators list for EmployeeId: {EmployeeId}", evaluatorRelationship.EmployeeId);
                continue;
            }

            _logger.LogInformation("✅ Found evaluator: {EvaluatorId} with EmployeeId: {EmployeeId}", evaluator.Id, evaluator.EmployeeIdString);

            // Validate self-evaluation: if relationship is "Self", subject and evaluator must reference the same employee
            if (string.Equals(evaluatorRelationship.RelationshipType, "Self", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(subject.Employee.EmployeeId, evaluator.EmployeeIdString, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("❌ Self-evaluation validation failed: Subject EmployeeId {SubjectEmployeeId} != Evaluator EmployeeId {EvaluatorEmployeeId}",
                        subject.Employee.EmployeeId, evaluator.EmployeeIdString);
                    result.FailedEmployeeIds.Add(evaluator.EmployeeIdString);
                    continue;
                }
            }

            if (existingRelationships.Contains(evaluator.Id))
            {
                _logger.LogInformation("⚠️ Duplicate relationship detected for evaluator {EmployeeId}", evaluator.EmployeeIdString);
                result.DuplicateConnections.Add(evaluator.EmployeeIdString);
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

            _logger.LogInformation("✅ Created relationship: Subject {SubjectId} -> Evaluator {EvaluatorId} ({EmployeeId}) as {RelationshipType}",
                subjectId, evaluator.Id, evaluator.EmployeeIdString, evaluatorRelationship.RelationshipType);
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
            .Include(s => s.Employee)
            .Where(s => subjectEmployeeIds.Contains(s.Employee.EmployeeId) && s.TenantId == tenantId)
            .Select(s => new { s.Id, EmployeeIdString = s.Employee.EmployeeId })
            .ToListAsync();

        // Check for non-existent EmployeeIds
        var foundEmployeeIds = existingSubjects.Select(s => s.EmployeeIdString).ToList();
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
                result.DuplicateConnections.Add(subject.EmployeeIdString);
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

        // Get evaluator's employee information for self-evaluation validation
        var evaluator = await _context.Evaluators
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.Id == evaluatorId && e.TenantId == tenantId);

        if (evaluator == null)
        {
            _logger.LogWarning("Evaluator {EvaluatorId} not found in tenant {TenantId}", evaluatorId, tenantId);
            return result;
        }

        var employeeIds = subjectRelationships.Select(sr => sr.EmployeeId).ToList();

        _logger.LogInformation("🔍 Searching for subjects with EmployeeIds: {EmployeeIds} in tenant {TenantId}",
            string.Join(", ", employeeIds), tenantId);

        // Get ALL subjects for this tenant first (for debugging)
        var allTenantSubjects = await _context.Subjects
            .Include(s => s.Employee)
            .Where(s => s.TenantId == tenantId)
            .Select(s => new { s.Id, EmployeeIdString = s.Employee.EmployeeId })
            .ToListAsync();

        _logger.LogInformation("📊 Total subjects in tenant {TenantId}: {Count}. EmployeeIds: {AllIds}",
            tenantId, allTenantSubjects.Count, string.Join(", ", allTenantSubjects.Select(s => s.EmployeeIdString)));

        // Get existing subjects by EmployeeId within the tenant (case-insensitive)
        var existingSubjects = allTenantSubjects
            .Where(s => employeeIds.Any(id => string.Equals(id, s.EmployeeIdString, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        _logger.LogInformation("✅ Found {Count} existing subjects for tenant {TenantId}: {SubjectIds}",
            existingSubjects.Count, tenantId, string.Join(", ", existingSubjects.Select(s => s.EmployeeIdString)));

        // Check for non-existent EmployeeIds
        var foundEmployeeIds = existingSubjects.Select(s => s.EmployeeIdString).ToList();
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
        _logger.LogInformation("🔄 Processing {Count} subject relationships", subjectRelationships.Count);
        foreach (var subjectRelationship in subjectRelationships)
        {
            _logger.LogInformation("🔍 Looking for subject with EmployeeId: {EmployeeId}", subjectRelationship.EmployeeId);

            var subject = existingSubjects.FirstOrDefault(s =>
                string.Equals(s.EmployeeIdString, subjectRelationship.EmployeeId, StringComparison.OrdinalIgnoreCase));
            if (subject == null)
            {
                _logger.LogWarning("❌ Subject not found in existingSubjects list for EmployeeId: {EmployeeId}", subjectRelationship.EmployeeId);
                continue;
            }

            _logger.LogInformation("✅ Found subject: {SubjectId} with EmployeeId: {EmployeeId}", subject.Id, subject.EmployeeIdString);

            // Validate self-evaluation: if relationship is "Self", subject and evaluator must reference the same employee
            if (string.Equals(subjectRelationship.RelationshipType, "Self", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(subject.EmployeeIdString, evaluator.Employee.EmployeeId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("❌ Self-evaluation validation failed: Subject EmployeeId {SubjectEmployeeId} != Evaluator EmployeeId {EvaluatorEmployeeId}",
                        subject.EmployeeIdString, evaluator.Employee.EmployeeId);
                    result.FailedEmployeeIds.Add(subject.EmployeeIdString);
                    continue;
                }
            }

            if (existingRelationships.Contains(subject.Id))
            {
                _logger.LogInformation("⚠️ Duplicate relationship detected for subject {EmployeeId}", subject.EmployeeIdString);
                result.DuplicateConnections.Add(subject.EmployeeIdString);
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

            _logger.LogInformation("✅ Created relationship: Evaluator {EvaluatorId} -> Subject {SubjectId} ({EmployeeId}) as {RelationshipType}",
                evaluatorId, subject.Id, subject.EmployeeIdString, subjectRelationship.RelationshipType);
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
                .ThenInclude(e => e.Employee)
            .Select(se => se.Evaluator.Employee.EmployeeId)
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

        // Resolve incoming evaluator employee IDs in this tenant (case-insensitive)
        var incomingEmployeeIds = newEvaluatorRelationships.Select(er => er.EmployeeId).ToList();
        var evaluators = await _context.Evaluators
            .Include(e => e.Employee)
            .Where(e => e.TenantId == tenantId && incomingEmployeeIds.Contains(e.Employee.EmployeeId))
            .Select(e => new { e.Id, EmployeeIdString = e.Employee.EmployeeId })
            .ToListAsync();

        var foundEmployeeIds = evaluators.Select(e => e.EmployeeIdString).ToList();
        result.FailedEmployeeIds = incomingEmployeeIds
            .Except(foundEmployeeIds, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Load existing relationships for this subject to these evaluators
        var existingRels = await _context.SubjectEvaluators
            .Where(se => se.SubjectId == subjectId && se.IsActive && evaluators.Select(e => e.Id).Contains(se.EvaluatorId))
            .Include(se => se.Evaluator)
                .ThenInclude(e => e.Employee)
            .ToListAsync();

        // Map existing by evaluator EmployeeId (case-insensitive)
        var existingByEmpId = existingRels
            .GroupBy(se => se.Evaluator.Employee.EmployeeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        int updates = 0;
        int creates = 0;

        foreach (var er in newEvaluatorRelationships)
        {
            var evaluator = evaluators.FirstOrDefault(ev => string.Equals(ev.EmployeeIdString, er.EmployeeId, StringComparison.OrdinalIgnoreCase));
            if (evaluator == null)
            {
                // Already accounted in FailedEmployeeIds
                continue;
            }

            if (existingByEmpId.TryGetValue(evaluator.EmployeeIdString, out var existing))
            {
                // Relationship exists; update type if changed
                if (!string.Equals(existing.Relationship, er.RelationshipType, StringComparison.OrdinalIgnoreCase))
                {
                    existing.Relationship = er.RelationshipType;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _context.SubjectEvaluators.Update(existing);
                    updates++;
                    result.SuccessfulConnections++;
                }
                else
                {
                    result.DuplicateConnections.Add(evaluator.EmployeeIdString);
                }
            }
            else
            {
                var relationship = new SubjectEvaluator
                {
                    Id = Guid.NewGuid(),
                    SubjectId = subjectId,
                    EvaluatorId = evaluator.Id,
                    Relationship = er.RelationshipType,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    TenantId = tenantId
                };

                _context.SubjectEvaluators.Add(relationship);
                creates++;
                result.SuccessfulConnections++;
            }
        }

        await _context.SaveChangesAsync();

        if (updates > 0)
        {
            result.Warnings.Add($"Updated relationship types for: {string.Join(", ", existingByEmpId.Keys.Where(k => newEvaluatorRelationships.Any(n => string.Equals(n.EmployeeId, k, StringComparison.OrdinalIgnoreCase))))}");
        }
        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Skipped unchanged relationships: {string.Join(", ", result.DuplicateConnections)}");
        }
        if (result.FailedEmployeeIds.Any())
        {
            result.Warnings.Add($"Evaluator EmployeeIds not found: {string.Join(", ", result.FailedEmployeeIds)}");
        }

        _logger.LogInformation("Merge completed for subject {SubjectId}: {Creates} created, {Updates} updated, {Duplicates} unchanged, {Failed} failed",
            subjectId, creates, updates, result.DuplicateConnections.Count, result.FailedEmployeeIds.Count);

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
                .ThenInclude(s => s.Employee)
            .Select(se => se.Subject.Employee.EmployeeId)
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

        // Resolve incoming subject employee IDs in this tenant (case-insensitive)
        var incomingEmployeeIds = newSubjectRelationships.Select(sr => sr.EmployeeId).ToList();
        var subjects = await _context.Subjects
            .Include(s => s.Employee)
            .Where(s => s.TenantId == tenantId && incomingEmployeeIds.Contains(s.Employee.EmployeeId))
            .Select(s => new { s.Id, EmployeeIdString = s.Employee.EmployeeId })
            .ToListAsync();

        var foundEmployeeIds = subjects.Select(s => s.EmployeeIdString).ToList();
        result.FailedEmployeeIds = incomingEmployeeIds
            .Except(foundEmployeeIds, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Load existing relationships for this evaluator to these subjects
        var existingRels = await _context.SubjectEvaluators
            .Where(se => se.EvaluatorId == evaluatorId && se.IsActive && subjects.Select(s => s.Id).Contains(se.SubjectId))
            .Include(se => se.Subject)
                .ThenInclude(s => s.Employee)
            .ToListAsync();

        // Map existing by subject EmployeeId (case-insensitive)
        var existingByEmpId = existingRels
            .GroupBy(se => se.Subject.Employee.EmployeeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        int updates = 0;
        int creates = 0;

        foreach (var sr in newSubjectRelationships)
        {
            var subject = subjects.FirstOrDefault(su => string.Equals(su.EmployeeIdString, sr.EmployeeId, StringComparison.OrdinalIgnoreCase));
            if (subject == null)
            {
                // Already accounted in FailedEmployeeIds
                continue;
            }

            if (existingByEmpId.TryGetValue(subject.EmployeeIdString, out var existing))
            {
                // Relationship exists; update type if changed
                if (!string.Equals(existing.Relationship, sr.RelationshipType, StringComparison.OrdinalIgnoreCase))
                {
                    existing.Relationship = sr.RelationshipType;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _context.SubjectEvaluators.Update(existing);
                    updates++;
                    result.SuccessfulConnections++;
                }
                else
                {
                    result.DuplicateConnections.Add(subject.EmployeeIdString);
                }
            }
            else
            {
                var relationship = new SubjectEvaluator
                {
                    Id = Guid.NewGuid(),
                    SubjectId = subject.Id,
                    EvaluatorId = evaluatorId,
                    Relationship = sr.RelationshipType,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    TenantId = tenantId
                };

                _context.SubjectEvaluators.Add(relationship);
                creates++;
                result.SuccessfulConnections++;
            }
        }

        await _context.SaveChangesAsync();

        if (updates > 0)
        {
            result.Warnings.Add($"Updated relationship types for: {string.Join(", ", existingByEmpId.Keys.Where(k => newSubjectRelationships.Any(n => string.Equals(n.EmployeeId, k, StringComparison.OrdinalIgnoreCase))))}");
        }
        if (result.DuplicateConnections.Any())
        {
            result.Warnings.Add($"Skipped unchanged relationships: {string.Join(", ", result.DuplicateConnections)}");
        }
        if (result.FailedEmployeeIds.Any())
        {
            result.Warnings.Add($"Subject EmployeeIds not found: {string.Join(", ", result.FailedEmployeeIds)}");
        }

        _logger.LogInformation("Merge completed for evaluator {EvaluatorId}: {Creates} created, {Updates} updated, {Duplicates} unchanged, {Failed} failed",
            evaluatorId, creates, updates, result.DuplicateConnections.Count, result.FailedEmployeeIds.Count);

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
                .Include(e => e.Employee)
                .Where(e => employeeIds.Contains(e.Employee.EmployeeId) && e.TenantId == tenantId)
                .Select(e => e.Employee.EmployeeId)
                .ToListAsync();
        }
        else
        {
            existingEmployeeIds = await _context.Subjects
                .Include(s => s.Employee)
                .Where(s => employeeIds.Contains(s.Employee.EmployeeId) && s.TenantId == tenantId)
                .Select(s => s.Employee.EmployeeId)
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
                .Include(e => e.Employee)
                .Where(e => employeeIds.Contains(e.Employee.EmployeeId) && e.TenantId == tenantId)
                .Select(e => new {
                    e.Id,
                    EmployeeIdGuid = e.EmployeeId,
                    EmployeeIdString = e.Employee.EmployeeId,
                    FirstName = e.Employee.FirstName,
                    LastName = e.Employee.LastName,
                    Email = e.Employee.Email,
                    e.IsActive
                })
                .ToListAsync();

            foreach (var employeeId in employeeIds)
            {
                var evaluator = evaluators.FirstOrDefault(e => e.EmployeeIdString == employeeId);
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
                            EmployeeId = evaluator.EmployeeIdString,
                            FullName = $"{evaluator.FirstName} {evaluator.LastName}",
                            Email = evaluator.Email,
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
                .Include(s => s.Employee)
                .Where(s => employeeIds.Contains(s.Employee.EmployeeId) && s.TenantId == tenantId)
                .Select(s => new {
                    s.Id,
                    EmployeeIdGuid = s.EmployeeId,
                    EmployeeIdString = s.Employee.EmployeeId,
                    FirstName = s.Employee.FirstName,
                    LastName = s.Employee.LastName,
                    Email = s.Employee.Email,
                    s.IsActive
                })
                .ToListAsync();

            foreach (var employeeId in employeeIds)
            {
                var subject = subjects.FirstOrDefault(s => s.EmployeeIdString == employeeId);
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
                            EmployeeId = subject.EmployeeIdString,
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
