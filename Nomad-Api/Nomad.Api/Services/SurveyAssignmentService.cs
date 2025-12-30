using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using BCrypt.Net;

namespace Nomad.Api.Services;

public class SurveyAssignmentService : ISurveyAssignmentService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<SurveyAssignmentService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private const string DefaultPassword = "Password@123";

    public SurveyAssignmentService(
        NomadSurveysDbContext context,
        ILogger<SurveyAssignmentService> logger,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
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

            var tenant = await _context.Tenants.FindAsync(survey.TenantId);
            var tenantName = tenant?.Name ?? "Nomad Surveys";
            var tenantSlug = tenant?.Slug ?? "";

            var assignedCount = 0;
            var pendingNotifications = new Dictionary<string, (string Name, string LastSubject, int Count, Guid LastId)>();

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

                var evaluatorEmail = relationship.Evaluator.Employee.Email;
                var evaluatorName = relationship.Evaluator.Employee.FullName;
                var subjectName = relationship.Subject.Employee.FullName;
                Guid finalAssignmentId;

                if (existingAssignment != null)
                {
                    if (!existingAssignment.IsActive)
                    {
                        // Reactivate existing assignment
                        existingAssignment.IsActive = true;
                        existingAssignment.UpdatedAt = DateTime.UtcNow;
                        assignedCount++;
                        finalAssignmentId = existingAssignment.Id;
                    }
                    else
                    {
                        errors.Add($"Relationship {relationship.Subject.Employee.FullName} - {relationship.Evaluator.Employee.FullName} is already assigned to this survey");
                        continue;
                    }
                }
                else
                {
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
                    finalAssignmentId = assignment.Id;
                }

                // Collect notification data
                if (pendingNotifications.TryGetValue(evaluatorEmail, out var data))
                {
                    pendingNotifications[evaluatorEmail] = (data.Name, subjectName, data.Count + 1, finalAssignmentId);
                }
                else
                {
                    pendingNotifications[evaluatorEmail] = (evaluatorName, subjectName, 1, finalAssignmentId);
                }
            }

            await _context.SaveChangesAsync();

            // Send grouped notifications
            NotifyEvaluators(pendingNotifications, survey.Title, tenantSlug, tenantName);

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

    public async Task<SurveyAssignmentResponse> AssignSurveyRelationshipsFromCsvAsync(Guid surveyId, AssignSurveyCsvRequest request)
    {
        var response = new SurveyAssignmentResponse();
        var errors = new List<string>();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && s.IsActive);

            if (survey == null)
            {
                response.Success = false;
                response.Message = "Survey not found";
                return response;
            }

            var tenant = await _context.Tenants.FindAsync(survey.TenantId);
            var tenantName = tenant?.Name ?? "Nomad Surveys";
            var tenantSlug = tenant?.Slug ?? "";

            var now = DateTime.UtcNow;
            var relationshipsProcessed = 0;
            var assignmentsCreatedOrReactivated = 0;
            var pendingNotifications = new Dictionary<string, (string Name, string LastSubject, int Count, Guid LastId)>();

            for (var index = 0; index < request.Rows.Count; index++)
            {
                var row = request.Rows[index];
                try
                {
                    var rowNumber = index + 2; // header offset

                    if (string.IsNullOrWhiteSpace(row.SubjectId) ||
                        string.IsNullOrWhiteSpace(row.EvaluatorId) ||
                        string.IsNullOrWhiteSpace(row.Relationship))
                    {
                        errors.Add($"Row {rowNumber}: EvaluatorId, SubjectId and Relationship are required.");
                        continue;
                    }

                    var subjectEmployeeId = row.SubjectId.Trim();
                    var evaluatorEmployeeId = row.EvaluatorId.Trim();
                    var relationship = row.Relationship.Trim();

                    if (relationship.Length == 0)
                    {
                        errors.Add($"Row {rowNumber}: Relationship cannot be empty.");
                        continue;
                    }

                    if (relationship.Length > 50)
                    {
                        errors.Add($"Row {rowNumber}: Relationship exceeds maximum length of 50 characters.");
                        continue;
                    }

                    // Step 1: Handle Evaluator - Check if employee exists, then check/create evaluator
                    var evaluatorEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == evaluatorEmployeeId && e.TenantId == survey.TenantId && e.IsActive);

                    if (evaluatorEmployee == null)
                    {
                        errors.Add($"Row {rowNumber}: Evaluator Employee '{evaluatorEmployeeId}' not found or is not active in this tenant.");
                        continue;
                    }

                    var existingEvaluator = await _context.Evaluators
                        .FirstOrDefaultAsync(e => e.EmployeeId == evaluatorEmployee.Id && e.TenantId == survey.TenantId);

                    Evaluator evaluator;
                    if (existingEvaluator != null)
                    {
                        if (!existingEvaluator.IsActive)
                        {
                            existingEvaluator.IsActive = true;
                            existingEvaluator.UpdatedAt = now;
                            _context.Evaluators.Update(existingEvaluator);
                            _logger.LogInformation("✅ Reactivated evaluator for employee {EmployeeId}", evaluatorEmployeeId);
                        }
                        evaluator = existingEvaluator;
                    }
                    else
                    {
                        evaluator = new Evaluator
                        {
                            Id = Guid.NewGuid(),
                            EmployeeId = evaluatorEmployee.Id,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                            IsActive = true,
                            CreatedAt = now,
                            TenantId = survey.TenantId
                        };
                        _context.Evaluators.Add(evaluator);
                        _logger.LogInformation("✅ Created evaluator for employee {EmployeeId}", evaluatorEmployeeId);
                    }

                    // Step 2: Handle Subject - Check if employee exists, then check/create subject
                    var subjectEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == subjectEmployeeId && e.TenantId == survey.TenantId && e.IsActive);

                    if (subjectEmployee == null)
                    {
                        errors.Add($"Row {rowNumber}: Subject Employee '{subjectEmployeeId}' not found or is not active in this tenant.");
                        continue;
                    }

                    var existingSubject = await _context.Subjects
                        .FirstOrDefaultAsync(s => s.EmployeeId == subjectEmployee.Id && s.TenantId == survey.TenantId);

                    Subject subject;
                    if (existingSubject != null)
                    {
                        if (!existingSubject.IsActive)
                        {
                            existingSubject.IsActive = true;
                            existingSubject.UpdatedAt = now;
                            _context.Subjects.Update(existingSubject);
                            _logger.LogInformation("✅ Reactivated subject for employee {EmployeeId}", subjectEmployeeId);
                        }
                        subject = existingSubject;
                    }
                    else
                    {
                        subject = new Subject
                        {
                            Id = Guid.NewGuid(),
                            EmployeeId = subjectEmployee.Id,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                            IsActive = true,
                            CreatedAt = now,
                            TenantId = survey.TenantId
                        };
                        _context.Subjects.Add(subject);
                        _logger.LogInformation("✅ Created subject for employee {EmployeeId}", subjectEmployeeId);
                    }

                    // Step 3: Handle SubjectEvaluator relationship
                    var existingRelationship = await _context.SubjectEvaluators
                        .FirstOrDefaultAsync(se => se.SubjectId == subject.Id && se.EvaluatorId == evaluator.Id);

                    SubjectEvaluator relationshipEntity;
                    if (existingRelationship != null)
                    {
                        if (!existingRelationship.IsActive)
                        {
                            existingRelationship.IsActive = true;
                            existingRelationship.UpdatedAt = now;
                            _logger.LogInformation("✅ Reactivated relationship: Subject {SubjectId} -> Evaluator {EvaluatorId}", subjectEmployeeId, evaluatorEmployeeId);
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ Relationship already active: Subject {SubjectId} -> Evaluator {EvaluatorId}", subjectEmployeeId, evaluatorEmployeeId);
                        }

                        // Update relationship type if different
                        if (!string.Equals(existingRelationship.Relationship, relationship, StringComparison.Ordinal))
                        {
                            existingRelationship.Relationship = relationship;
                            existingRelationship.UpdatedAt = now;
                        }

                        relationshipEntity = existingRelationship;
                    }
                    else
                    {
                        relationshipEntity = new SubjectEvaluator
                        {
                            Id = Guid.NewGuid(),
                            SubjectId = subject.Id,
                            EvaluatorId = evaluator.Id,
                            Relationship = relationship,
                            TenantId = survey.TenantId,
                            IsActive = true,
                            CreatedAt = now
                        };
                        _context.SubjectEvaluators.Add(relationshipEntity);
                        _logger.LogInformation("✅ Created relationship: Subject {SubjectId} -> Evaluator {EvaluatorId} as {Relationship}", subjectEmployeeId, evaluatorEmployeeId, relationship);
                    }

                    relationshipsProcessed++;

                    // Step 4: Handle SubjectEvaluatorSurvey assignment
                    var existingAssignmentInfo = await _context.SubjectEvaluatorSurveys
                        .FirstOrDefaultAsync(ses => ses.SubjectEvaluatorId == relationshipEntity.Id && ses.SurveyId == surveyId);

                    Guid finalAssignmentId;
                    bool notificationNeeded = false;

                    if (existingAssignmentInfo != null)
                    {
                        if (!existingAssignmentInfo.IsActive)
                        {
                            existingAssignmentInfo.IsActive = true;
                            existingAssignmentInfo.UpdatedAt = now;
                            assignmentsCreatedOrReactivated++;
                            _logger.LogInformation("✅ Reactivated survey assignment for relationship {RelationshipId}", relationshipEntity.Id);
                            finalAssignmentId = existingAssignmentInfo.Id;
                            notificationNeeded = true;
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ Survey assignment already active for relationship {RelationshipId}", relationshipEntity.Id);
                            continue;
                        }
                    }
                    else
                    {
                        var newAssignment = new SubjectEvaluatorSurvey
                        {
                            Id = Guid.NewGuid(),
                            SubjectEvaluatorId = relationshipEntity.Id,
                            SurveyId = surveyId,
                            TenantId = survey.TenantId,
                            CreatedAt = now,
                            IsActive = true
                        };
                        _context.SubjectEvaluatorSurveys.Add(newAssignment);
                        assignmentsCreatedOrReactivated++;
                        _logger.LogInformation("✅ Created survey assignment for relationship {RelationshipId}", relationshipEntity.Id);
                        finalAssignmentId = newAssignment.Id;
                        notificationNeeded = true;
                    }

                    if (notificationNeeded)
                    {
                        var evaluatorEmail = evaluatorEmployee.Email;
                        var evaluatorName = evaluatorEmployee.FullName;
                        var subjectName = subjectEmployee.FullName;

                        if (pendingNotifications.TryGetValue(evaluatorEmail, out var data))
                        {
                            pendingNotifications[evaluatorEmail] = (data.Name, subjectName, data.Count + 1, finalAssignmentId);
                        }
                        else
                        {
                            pendingNotifications[evaluatorEmail] = (evaluatorName, subjectName, 1, finalAssignmentId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CSV row {RowNumber} for survey {SurveyId}", index + 2, surveyId);
                    errors.Add($"Row {index + 2}: Error processing row - {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send grouped notifications
            NotifyEvaluators(pendingNotifications, survey.Title, tenantSlug, tenantName);

            response.Success = assignmentsCreatedOrReactivated > 0 || relationshipsProcessed > 0;
            response.AssignedCount = assignmentsCreatedOrReactivated;
            response.ErrorCount = errors.Count;
            response.Errors = errors;
            response.Message = assignmentsCreatedOrReactivated > 0
                ? $"Successfully assigned {assignmentsCreatedOrReactivated} relationship(s) from CSV. {errors.Count} error(s) occurred."
                : relationshipsProcessed > 0
                    ? $"CSV processed: {relationshipsProcessed} relationship(s) processed but all were already assigned to this survey. {errors.Count} error(s) occurred."
                    : $"No relationships were processed. {errors.Count} error(s) occurred.";

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error assigning survey {SurveyId} relationships from CSV", surveyId);
            response.Success = false;
            response.Message = "An error occurred while assigning survey relationships from CSV";
            response.Errors.Add(ex.Message);
            response.ErrorCount = response.Errors.Count;
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

    private void NotifyEvaluators(Dictionary<string, (string Name, string LastSubject, int Count, Guid LastId)> notifications, string formTitle, string tenantSlug, string tenantName)
    {
        foreach (var entry in notifications)
        {
            var email = entry.Key;
            var data = entry.Value;

            _ = Task.Run(async () =>
            {
                try
                {
                    var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
                    
                    if (data.Count == 1)
                    {
                        var formLink = $"{frontendUrl}/{tenantSlug}/participant/forms/{data.LastId}";
                        await _emailService.SendFormAssignmentEmailAsync(email, data.Name, data.LastSubject, formTitle, formLink, tenantName);
                    }
                    else
                    {
                        var dashboardLink = $"{frontendUrl}/{tenantSlug}/participant/forms";
                        await _emailService.SendBulkFormAssignmentEmailAsync(email, data.Name, data.Count, formTitle, dashboardLink, tenantName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send assignment notification to {Email}", email);
                }
            });
        }
    }
}

