using Nomad.Api.Data;
using Nomad.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nomad.Api.Services;

public interface IRelationshipService
{
    Task<RelationshipResult> CreateSubjectEvaluatorRelationshipsAsync(
        Guid subjectId, 
        List<string> evaluatorEmployeeIds, 
        Guid tenantId);
    
    Task<RelationshipResult> CreateEvaluatorSubjectRelationshipsAsync(
        Guid evaluatorId, 
        List<string> subjectEmployeeIds, 
        Guid tenantId);
    
    Task<List<string>> ValidateEmployeeIdsAsync(
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
        var existingEvaluators = await _context.Evaluators
            .Where(e => evaluatorEmployeeIds.Contains(e.EmployeeId) && e.TenantId == tenantId)
            .Select(e => new { e.Id, e.EmployeeId })
            .ToListAsync();

        _logger.LogInformation("Found {Count} existing evaluators for tenant {TenantId}: {EvaluatorIds}",
            existingEvaluators.Count, tenantId, string.Join(", ", existingEvaluators.Select(e => e.EmployeeId)));

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
}
