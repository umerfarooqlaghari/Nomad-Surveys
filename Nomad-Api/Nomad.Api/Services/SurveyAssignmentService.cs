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
    private readonly IPasswordGenerator _passwordGenerator;

    public SurveyAssignmentService(
        NomadSurveysDbContext context,
        ILogger<SurveyAssignmentService> logger,
        IEmailService emailService,
        IConfiguration configuration,
        IPasswordGenerator passwordGenerator)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _passwordGenerator = passwordGenerator;
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
            var pendingNotifications = new Dictionary<string, (string Name, string LastSubject, int Count, Guid LastId, string PasswordHash)>();

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

                var evaluatorPasswordHash = relationship.Evaluator.PasswordHash;

                // Collect notification data
                if (pendingNotifications.TryGetValue(evaluatorEmail, out var data))
                {
                    pendingNotifications[evaluatorEmail] = (data.Name, subjectName, data.Count + 1, finalAssignmentId, data.PasswordHash);
                }
                else
                {
                    pendingNotifications[evaluatorEmail] = (evaluatorName, subjectName, 1, finalAssignmentId, evaluatorPasswordHash);
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
            var pendingNotifications = new Dictionary<string, (string Name, string LastSubject, int Count, Guid LastId, string PasswordHash)>();

            // 1. Pre-fetch Analysis: Extract all unique IDs
            var allSubjectIds = request.Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.SubjectId))
                .Select(r => r.SubjectId.Trim())
                .Distinct()
                .ToList();

            var allEvaluatorIds = request.Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.EvaluatorId))
                .Select(r => r.EvaluatorId.Trim())
                .Distinct()
                .ToList();

            var allEmployeeIds = allSubjectIds.Union(allEvaluatorIds).Distinct().ToList();

            // 2. Bulk Fetch Employees
            var employees = await _context.Employees
                .Where(e => allEmployeeIds.Contains(e.EmployeeId) && e.TenantId == survey.TenantId && e.IsActive)
                .ToDictionaryAsync(e => e.EmployeeId, e => e);

            // 3. Bulk Fetch Existing Subjects & Evaluators
            // Note: We fetch ALL subjects/evaluators involved in this batch to avoid N+1 queries
            // and to populate our local cache for reuse.
            var existingSubjects = await _context.Subjects
                .Include(s => s.Employee)
                .Where(s => allSubjectIds.Contains(s.Employee.EmployeeId) && s.TenantId == survey.TenantId)
                .ToListAsync();

            var existingEvaluators = await _context.Evaluators
                .Include(e => e.Employee)
                .Where(e => allEvaluatorIds.Contains(e.Employee.EmployeeId) && e.TenantId == survey.TenantId)
                .ToListAsync();

            // 4. Initialize Local Cache (Dictionaries)
            // Keys are EmployeeId strings
            var subjectMap = existingSubjects.ToDictionary(s => s.Employee.EmployeeId, s => s);
            var evaluatorMap = existingEvaluators.ToDictionary(e => e.Employee.EmployeeId, e => e);

            // We also need to cache relationships to avoid duplicates. 
            // Key: "SubjectId_EvaluatorId" (Using GUIDs)
            // We'll fetch existing relationships for the found subjects/evaluators
            var subjectGuids = existingSubjects.Select(s => s.Id).ToList();
            var evaluatorGuids = existingEvaluators.Select(e => e.Id).ToList();

            var existingRelationships = await _context.SubjectEvaluators
                .Where(se => subjectGuids.Contains(se.SubjectId) || evaluatorGuids.Contains(se.EvaluatorId))
                .Where(se => se.TenantId == survey.TenantId)
                .ToListAsync();

            // Map Key: Tuple (SubjectGuid, EvaluatorGuid)
            var relationshipMap = existingRelationships
                .ToDictionary(se => (se.SubjectId, se.EvaluatorId), se => se);

            // Fetch existing assignments for these relationships
            var relationshipGuids = existingRelationships.Select(se => se.Id).ToList();
            var existingAssignments = await _context.SubjectEvaluatorSurveys
                .Where(ses => ses.SurveyId == surveyId && (relationshipGuids.Contains(ses.SubjectEvaluatorId))) // We'll double check locally for newly created ones
                .ToListAsync();
            
            // Map Key: SubjectEvaluatorGuid
            var assignmentMap = existingAssignments
                .ToDictionary(ses => ses.SubjectEvaluatorId, ses => ses);


            // 5. Iterate Rows with Local Cache
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
                    var relationshipType = row.Relationship.Trim();

                    if (relationshipType.Length == 0 || relationshipType.Length > 50)
                    {
                        errors.Add($"Row {rowNumber}: Relationship valid/length check failed.");
                        continue;
                    }

                    // --- STEP A: RESOLVE EMPLOYEES ---
                    if (!employees.TryGetValue(subjectEmployeeId, out var subjectEmployee))
                    {
                        errors.Add($"Row {rowNumber}: Subject Employee '{subjectEmployeeId}' not found.");
                        continue;
                    }
                    if (!employees.TryGetValue(evaluatorEmployeeId, out var evaluatorEmployee))
                    {
                        errors.Add($"Row {rowNumber}: Evaluator Employee '{evaluatorEmployeeId}' not found.");
                        continue;
                    }

                    // --- STEP B: RESOLVE/CREATE SUBJECT (Reuse from Map) ---
                    if (!subjectMap.TryGetValue(subjectEmployeeId, out var subject))
                    {
                        subject = new Subject
                        {
                            Id = Guid.NewGuid(),
                            EmployeeId = subjectEmployee.Id,
                            Employee = subjectEmployee, // Link for safety
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_passwordGenerator.Generate(subjectEmployee.Email)),
                            IsActive = true,
                            CreatedAt = now,
                            TenantId = survey.TenantId
                        };
                        _context.Subjects.Add(subject);
                        subjectMap[subjectEmployeeId] = subject; // Add to cache immediately
                        _logger.LogInformation("✅ Created subject for employee {EmployeeId}", subjectEmployeeId);
                    }
                    else if (!subject.IsActive)
                    {
                        subject.IsActive = true;
                        subject.UpdatedAt = now;
                        // No need to Add, it's tracked. changing property sets state to Modified.
                    }

                    // --- STEP C: RESOLVE/CREATE EVALUATOR (Reuse from Map) ---
                    if (!evaluatorMap.TryGetValue(evaluatorEmployeeId, out var evaluator))
                    {
                        evaluator = new Evaluator
                        {
                            Id = Guid.NewGuid(),
                            EmployeeId = evaluatorEmployee.Id,
                            Employee = evaluatorEmployee, // Link for safety
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_passwordGenerator.Generate(evaluatorEmployee.Email)),
                            IsActive = true,
                            CreatedAt = now,
                            TenantId = survey.TenantId
                        };
                        _context.Evaluators.Add(evaluator);
                        evaluatorMap[evaluatorEmployeeId] = evaluator; // Add to cache immediately
                        _logger.LogInformation("✅ Created evaluator for employee {EmployeeId}", evaluatorEmployeeId);
                    }
                    else if (!evaluator.IsActive)
                    {
                        evaluator.IsActive = true;
                        evaluator.UpdatedAt = now;
                    }

                    // --- STEP D: RESOLVE/CREATE RELATIONSHIP (Reuse from Map) ---
                    var relKey = (subject.Id, evaluator.Id);
                    if (!relationshipMap.TryGetValue(relKey, out var relationshipEntity))
                    {
                        relationshipEntity = new SubjectEvaluator
                        {
                            Id = Guid.NewGuid(),
                            SubjectId = subject.Id,
                            EvaluatorId = evaluator.Id,
                            Relationship = relationshipType,
                            TenantId = survey.TenantId,
                            IsActive = true,
                            CreatedAt = now
                        };
                        _context.SubjectEvaluators.Add(relationshipEntity);
                        relationshipMap[relKey] = relationshipEntity; // Add to cache
                        _logger.LogInformation("✅ Created relationship: {Subject} -> {Evaluator}", subjectEmployeeId, evaluatorEmployeeId);
                    }
                    else
                    {
                        if (!relationshipEntity.IsActive)
                        {
                            relationshipEntity.IsActive = true;
                            relationshipEntity.UpdatedAt = now;
                        }
                        // Update relationship type if different
                        if (!string.Equals(relationshipEntity.Relationship, relationshipType, StringComparison.Ordinal))
                        {
                            relationshipEntity.Relationship = relationshipType;
                            relationshipEntity.UpdatedAt = now;
                        }
                    }

                    relationshipsProcessed++;

                    // --- STEP E: RESOLVE/CREATE ASSIGNMENT (Reuse from Map) ---
                    var assignKey = relationshipEntity.Id;
                    bool notificationNeeded = false;
                    Guid finalAssignmentId;

                    if (assignmentMap.TryGetValue(assignKey, out var existingAssignment))
                    {
                         if (!existingAssignment.IsActive)
                        {
                            existingAssignment.IsActive = true;
                            existingAssignment.UpdatedAt = now;
                            assignmentsCreatedOrReactivated++;
                            finalAssignmentId = existingAssignment.Id;
                            notificationNeeded = true;
                        }
                        else
                        {
                            // Already exists and active, skip
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
                        assignmentMap[assignKey] = newAssignment; // Add to cache
                        assignmentsCreatedOrReactivated++;
                        finalAssignmentId = newAssignment.Id;
                        notificationNeeded = true;
                    }

                    if (notificationNeeded)
                    {
                        var evaluatorPasswordHash = evaluator.PasswordHash;
                        if (pendingNotifications.TryGetValue(evaluatorEmployee.Email, out var data))
                        {
                            pendingNotifications[evaluatorEmployee.Email] = (data.Name, subjectEmployee.FullName, data.Count + 1, finalAssignmentId, data.PasswordHash);
                        }
                        else
                        {
                            pendingNotifications[evaluatorEmployee.Email] = (evaluatorEmployee.FullName, subjectEmployee.FullName, 1, finalAssignmentId, evaluatorPasswordHash);
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CSV row {RowNumber}", index + 2);
                    errors.Add($"Row {index + 2}: Error - {ex.Message}");
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
                    ? $"CSV processed: {relationshipsProcessed} relationship(s) processed but all were already assigned. {errors.Count} error(s) occurred."
                    : $"No valid relationships processed. {errors.Count} error(s) occurred.";

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "CSV Import Fatal Error");
            response.Success = false;
            response.Message = "Fatal error during CSV import";
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

    private void NotifyEvaluators(Dictionary<string, (string Name, string LastSubject, int Count, Guid LastId, string PasswordHash)> notifications, string formTitle, string tenantSlug, string tenantName)
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
                    var generatedPassword = _passwordGenerator.Generate(email);
                    var isDefaultPassword = BCrypt.Net.BCrypt.Verify(generatedPassword, data.PasswordHash);
                    var passwordDisplay = isDefaultPassword ? generatedPassword : "omitted for privacy";
                    
                    if (data.Count == 1)
                    {
                        // var formLink = $"{frontendUrl}/{tenantSlug}/participant/forms/{data.LastId}";
                                                var formLink = $"{frontendUrl}";
                        await _emailService.SendFormAssignmentEmailAsync(email, data.Name, data.LastSubject, formTitle, formLink, tenantName, tenantSlug, passwordDisplay);
                    }
                    else
                    {
                        var dashboardLink = $"{frontendUrl}/{tenantSlug}/participant/forms";
                        await _emailService.SendBulkFormAssignmentEmailAsync(email, data.Name, data.Count, formTitle, dashboardLink, tenantName, tenantSlug, passwordDisplay);
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

