using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class SurveyAssignmentService : ISurveyAssignmentService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<SurveyAssignmentService> _logger;

    public SurveyAssignmentService(NomadSurveysDbContext context, ILogger<SurveyAssignmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SurveyAssignmentResponse> AssignSurveyToRelationshipsAsync(Guid surveyId, AssignSurveyRequest request)
    {
        var response = new SurveyAssignmentResponse();
        var errors = new List<string>();

        try
        {
            // Verify survey exists
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && s.IsActive);

            if (survey == null)
            {
                response.Success = false;
                response.Message = "Survey not found";
                return response;
            }

            var assignedCount = 0;

            foreach (var subjectEvaluatorId in request.SubjectEvaluatorIds)
            {
                // Verify SubjectEvaluator relationship exists
                var relationship = await _context.SubjectEvaluators
                    .Include(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                    .Include(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                    .FirstOrDefaultAsync(se => se.Id == subjectEvaluatorId && se.IsActive);

                if (relationship == null)
                {
                    errors.Add($"Subject-Evaluator relationship {subjectEvaluatorId} not found");
                    continue;
                }

                // Validate assignment based on survey type and relationship type
                var validationError = ValidateAssignment(survey, relationship);
                if (validationError != null)
                {
                    errors.Add(validationError);
                    continue;
                }

                // Check if assignment already exists
                var existingAssignment = await _context.SubjectEvaluatorSurveys
                    .FirstOrDefaultAsync(ses => ses.SubjectEvaluatorId == subjectEvaluatorId && 
                                               ses.SurveyId == surveyId);

                if (existingAssignment != null)
                {
                    if (!existingAssignment.IsActive)
                    {
                        // Reactivate existing assignment
                        existingAssignment.IsActive = true;
                        existingAssignment.UpdatedAt = DateTime.UtcNow;
                        assignedCount++;
                    }
                    else
                    {
                        errors.Add($"Relationship {relationship.Subject.Employee.FullName} - {relationship.Evaluator.Employee.FullName} is already assigned to this survey");
                    }
                    continue;
                }

                // Create new assignment
                var assignment = new SubjectEvaluatorSurvey
                {
                    Id = Guid.NewGuid(),
                    SubjectEvaluatorId = subjectEvaluatorId,
                    SurveyId = surveyId,
                    TenantId = survey.TenantId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubjectEvaluatorSurveys.Add(assignment);
                assignedCount++;
            }

            await _context.SaveChangesAsync();

            response.Success = true;
            response.AssignedCount = assignedCount;
            response.ErrorCount = errors.Count;
            response.Errors = errors;
            response.Message = $"Successfully assigned {assignedCount} relationship(s). {errors.Count} error(s) occurred.";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning survey to relationships");
            response.Success = false;
            response.Message = "An error occurred while assigning survey";
            response.Errors.Add(ex.Message);
            return response;
        }
    }

    public async Task<SurveyAssignmentResponse> UnassignSurveyFromRelationshipsAsync(Guid surveyId, UnassignSurveyRequest request)
    {
        var response = new SurveyAssignmentResponse();
        var errors = new List<string>();

        try
        {
            var unassignedCount = 0;

            foreach (var subjectEvaluatorId in request.SubjectEvaluatorIds)
            {
                var assignment = await _context.SubjectEvaluatorSurveys
                    .FirstOrDefaultAsync(ses => ses.SubjectEvaluatorId == subjectEvaluatorId && 
                                               ses.SurveyId == surveyId &&
                                               ses.IsActive);

                if (assignment == null)
                {
                    errors.Add($"Assignment for relationship {subjectEvaluatorId} not found");
                    continue;
                }

                assignment.IsActive = false;
                assignment.UpdatedAt = DateTime.UtcNow;
                unassignedCount++;
            }

            await _context.SaveChangesAsync();

            response.Success = true;
            response.UnassignedCount = unassignedCount;
            response.ErrorCount = errors.Count;
            response.Errors = errors;
            response.Message = $"Successfully unassigned {unassignedCount} relationship(s). {errors.Count} error(s) occurred.";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning survey from relationships");
            response.Success = false;
            response.Message = "An error occurred while unassigning survey";
            response.Errors.Add(ex.Message);
            return response;
        }
    }

    public async Task<List<AssignedRelationshipResponse>> GetAssignedRelationshipsAsync(Guid surveyId, string? search = null, string? relationshipType = null)
    {
        try
        {
            var query = _context.SubjectEvaluatorSurveys
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                .Where(ses => ses.SurveyId == surveyId && ses.IsActive);

            // Apply relationship type filter
            if (!string.IsNullOrWhiteSpace(relationshipType))
            {
                query = query.Where(ses => ses.SubjectEvaluator.Relationship == relationshipType);
            }

            var assignments = await query.ToListAsync();

            // Apply search filter in memory (after loading from DB)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                assignments = assignments.Where(ses =>
                    ses.SubjectEvaluator.Subject.Employee.FullName.ToLower().Contains(searchLower) ||
                    ses.SubjectEvaluator.Evaluator.Employee.FullName.ToLower().Contains(searchLower) ||
                    ses.SubjectEvaluator.Subject.Employee.EmployeeId.ToLower().Contains(searchLower) ||
                    ses.SubjectEvaluator.Evaluator.Employee.EmployeeId.ToLower().Contains(searchLower) ||
                    (ses.SubjectEvaluator.Subject.Employee.Designation?.ToLower().Contains(searchLower) ?? false) ||
                    (ses.SubjectEvaluator.Evaluator.Employee.Designation?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }

            return assignments.Select(ses => new AssignedRelationshipResponse
            {
                Id = ses.Id,
                SubjectEvaluatorId = ses.SubjectEvaluatorId,
                Relationship = ses.SubjectEvaluator.Relationship,
                SubjectId = ses.SubjectEvaluator.SubjectId,
                SubjectFullName = ses.SubjectEvaluator.Subject.Employee.FullName,
                SubjectEmail = ses.SubjectEvaluator.Subject.Employee.Email,
                SubjectEmployeeIdString = ses.SubjectEvaluator.Subject.Employee.EmployeeId,
                SubjectDesignation = ses.SubjectEvaluator.Subject.Employee.Designation,
                EvaluatorId = ses.SubjectEvaluator.EvaluatorId,
                EvaluatorFullName = ses.SubjectEvaluator.Evaluator.Employee.FullName,
                EvaluatorEmail = ses.SubjectEvaluator.Evaluator.Employee.Email,
                EvaluatorEmployeeIdString = ses.SubjectEvaluator.Evaluator.Employee.EmployeeId,
                EvaluatorDesignation = ses.SubjectEvaluator.Evaluator.Employee.Designation,
                IsActive = ses.IsActive,
                CreatedAt = ses.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned relationships for survey {SurveyId}", surveyId);
            return new List<AssignedRelationshipResponse>();
        }
    }

    public async Task<List<AvailableRelationshipResponse>> GetAvailableRelationshipsAsync(Guid surveyId, string? search = null, string? relationshipType = null)
    {
        try
        {
            // Get the survey to verify it exists
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && s.IsActive);

            if (survey == null)
            {
                return new List<AvailableRelationshipResponse>();
            }

            // Get all active SubjectEvaluator relationships
            var query = _context.SubjectEvaluators
                .Include(se => se.Subject)
                    .ThenInclude(s => s.Employee)
                .Include(se => se.Evaluator)
                    .ThenInclude(e => e.Employee)
                .Where(se => se.IsActive);

            // Apply relationship type filter
            if (!string.IsNullOrWhiteSpace(relationshipType))
            {
                query = query.Where(se => se.Relationship == relationshipType);
            }

            var allRelationships = await query.ToListAsync();

            // Get already assigned relationship IDs
            var assignedIds = await _context.SubjectEvaluatorSurveys
                .Where(ses => ses.SurveyId == surveyId && ses.IsActive)
                .Select(ses => ses.SubjectEvaluatorId)
                .ToListAsync();

            // Filter out already assigned relationships
            var availableRelationships = allRelationships
                .Where(se => !assignedIds.Contains(se.Id))
                .ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                availableRelationships = availableRelationships.Where(se =>
                    se.Subject.Employee.FullName.ToLower().Contains(searchLower) ||
                    se.Evaluator.Employee.FullName.ToLower().Contains(searchLower) ||
                    se.Subject.Employee.EmployeeId.ToLower().Contains(searchLower) ||
                    se.Evaluator.Employee.EmployeeId.ToLower().Contains(searchLower) ||
                    (se.Subject.Employee.Designation?.ToLower().Contains(searchLower) ?? false) ||
                    (se.Evaluator.Employee.Designation?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }

            return availableRelationships.Select(se => new AvailableRelationshipResponse
            {
                SubjectEvaluatorId = se.Id,
                Relationship = se.Relationship,
                SubjectId = se.SubjectId,
                SubjectFullName = se.Subject.Employee.FullName,
                SubjectEmail = se.Subject.Employee.Email,
                SubjectEmployeeIdString = se.Subject.Employee.EmployeeId,
                SubjectDesignation = se.Subject.Employee.Designation,
                EvaluatorId = se.EvaluatorId,
                EvaluatorFullName = se.Evaluator.Employee.FullName,
                EvaluatorEmail = se.Evaluator.Employee.Email,
                EvaluatorEmployeeIdString = se.Evaluator.Employee.EmployeeId,
                EvaluatorDesignation = se.Evaluator.Employee.Designation,
                IsActive = se.IsActive
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available relationships for survey {SurveyId}", surveyId);
            return new List<AvailableRelationshipResponse>();
        }
    }

    public async Task<int> GetAssignmentCountAsync(Guid surveyId)
    {
        try
        {
            return await _context.SubjectEvaluatorSurveys
                .Where(ses => ses.SurveyId == surveyId && ses.IsActive)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignment count for survey {SurveyId}", surveyId);
            return 0;
        }
    }

    private string? ValidateAssignment(Survey survey, SubjectEvaluator relationship)
    {
        // No validation needed - surveys now support all relationship types
        // Questions will be conditionally displayed based on relationship type at runtime
        return null;
    }
}

