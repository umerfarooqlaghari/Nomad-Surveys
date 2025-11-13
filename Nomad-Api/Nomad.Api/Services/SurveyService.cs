using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using System.Text.Json;

namespace Nomad.Api.Services;

public class SurveyService : ISurveyService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<SurveyService> _logger;

    public SurveyService(NomadSurveysDbContext context, ILogger<SurveyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SurveyListResponse>> GetSurveysAsync(Guid? tenantId = null)
    {
        try
        {
            var query = _context.Surveys.AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(s => s.TenantId == tenantId.Value);
            }

            var surveys = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return surveys.Select(s => new SurveyListResponse
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                QuestionCount = CalculateQuestionCount(s.Schema)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving surveys for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<SurveyResponse?> GetSurveyByIdAsync(Guid surveyId)
    {
        try
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId);

            if (survey == null)
            {
                return null;
            }

            return new SurveyResponse
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                Schema = survey.Schema,
                IsActive = survey.IsActive,
                CreatedAt = survey.CreatedAt,
                UpdatedAt = survey.UpdatedAt,
                TenantId = survey.TenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving survey {SurveyId}", surveyId);
            throw;
        }
    }

    public async Task<SurveyResponse> CreateSurveyAsync(CreateSurveyRequest request, Guid tenantId)
    {
        try
        {
            // Convert schema object to JsonDocument
            var schemaJson = JsonSerializer.Serialize(request.Schema);
            var schemaDocument = JsonDocument.Parse(schemaJson);

            var survey = new Survey
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Schema = schemaDocument,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Survey created successfully: {SurveyId} for tenant {TenantId}", survey.Id, tenantId);

            // Auto-assign to all relationships if requested
            if (request.AutoAssign)
            {
                _logger.LogInformation("Auto-assigning survey {SurveyId} to all active relationships", survey.Id);
                var assignmentResult = await AutoAssignSurveyToAllRelationshipsAsync(survey.Id, tenantId);
                _logger.LogInformation("Auto-assignment completed: {AssignedCount} relationships assigned, {ErrorCount} errors",
                    assignmentResult.AssignedCount, assignmentResult.ErrorCount);
            }

            return new SurveyResponse
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                Schema = survey.Schema,
                IsActive = survey.IsActive,
                CreatedAt = survey.CreatedAt,
                UpdatedAt = survey.UpdatedAt,
                TenantId = survey.TenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating survey for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<SurveyResponse?> UpdateSurveyAsync(Guid surveyId, UpdateSurveyRequest request)
    {
        try
        {
            var survey = await _context.Surveys.FirstOrDefaultAsync(s => s.Id == surveyId);

            if (survey == null)
            {
                return null;
            }

            survey.Title = request.Title;
            survey.Description = request.Description;

            // Convert schema object to JsonDocument
            var schemaJson = JsonSerializer.Serialize(request.Schema);
            survey.Schema = JsonDocument.Parse(schemaJson);

            if (request.IsActive.HasValue)
            {
                survey.IsActive = request.IsActive.Value;
            }

            survey.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Survey updated successfully: {SurveyId}", surveyId);

            return new SurveyResponse
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                Schema = survey.Schema,
                IsActive = survey.IsActive,
                CreatedAt = survey.CreatedAt,
                UpdatedAt = survey.UpdatedAt,
                TenantId = survey.TenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating survey {SurveyId}", surveyId);
            throw;
        }
    }

    public async Task<bool> DeleteSurveyAsync(Guid surveyId)
    {
        try
        {
            var survey = await _context.Surveys.FirstOrDefaultAsync(s => s.Id == surveyId);

            if (survey == null)
            {
                return false;
            }

            _context.Surveys.Remove(survey);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Survey deleted successfully: {SurveyId}", surveyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting survey {SurveyId}", surveyId);
            throw;
        }
    }

    public async Task<bool> SurveyExistsAsync(Guid surveyId, Guid? tenantId = null)
    {
        try
        {
            var query = _context.Surveys.Where(s => s.Id == surveyId);

            if (tenantId.HasValue)
            {
                query = query.Where(s => s.TenantId == tenantId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if survey exists {SurveyId}", surveyId);
            throw;
        }
    }

    /// <summary>
    /// Auto-assign survey to all active subject-evaluator relationships in the tenant
    /// Performs bulk insert with single SaveChanges call for efficiency
    /// </summary>
    public async Task<SurveyAssignmentResponse> AutoAssignSurveyToAllRelationshipsAsync(Guid surveyId, Guid tenantId)
    {
        var response = new SurveyAssignmentResponse();

        try
        {
            // Verify survey exists and belongs to tenant
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && s.TenantId == tenantId && s.IsActive);

            if (survey == null)
            {
                response.Success = false;
                response.Message = "Survey not found or inactive";
                return response;
            }

            // Fetch all active subject-evaluator relationships for this tenant in a single query
            var activeRelationships = await _context.SubjectEvaluators
                .Where(se => se.TenantId == tenantId && se.IsActive)
                .Select(se => new { se.Id, se.TenantId })
                .ToListAsync();

            // Edge case: No relationships exist
            if (!activeRelationships.Any())
            {
                response.Success = true;
                response.Message = "No active subject-evaluator relationships found for this tenant";
                response.AssignedCount = 0;
                _logger.LogWarning("Auto-assign: No active relationships found for tenant {TenantId}", tenantId);
                return response;
            }

            // Fetch existing assignments for this survey in a single query to avoid duplicates
            var existingAssignmentIds = await _context.SubjectEvaluatorSurveys
                .Where(ses => ses.SurveyId == surveyId && ses.TenantId == tenantId)
                .Select(ses => ses.SubjectEvaluatorId)
                .ToHashSetAsync();

            // Prepare new assignments (excluding duplicates)
            var newAssignments = new List<SubjectEvaluatorSurvey>();
            var skippedDuplicates = 0;
            var now = DateTime.UtcNow;

            foreach (var relationship in activeRelationships)
            {
                // Edge case: Skip if already assigned (duplicate prevention)
                if (existingAssignmentIds.Contains(relationship.Id))
                {
                    skippedDuplicates++;
                    continue;
                }

                newAssignments.Add(new SubjectEvaluatorSurvey
                {
                    Id = Guid.NewGuid(),
                    SubjectEvaluatorId = relationship.Id,
                    SurveyId = surveyId,
                    TenantId = tenantId,
                    IsActive = true,
                    CreatedAt = now
                });
            }

            // Edge case: All relationships already assigned
            if (!newAssignments.Any())
            {
                response.Success = true;
                response.Message = $"All {activeRelationships.Count} relationships are already assigned to this survey";
                response.AssignedCount = 0;
                _logger.LogInformation("Auto-assign: All relationships already assigned for survey {SurveyId}", surveyId);
                return response;
            }

            // Bulk insert with single SaveChanges call
            _context.SubjectEvaluatorSurveys.AddRange(newAssignments);
            await _context.SaveChangesAsync();

            response.Success = true;
            response.AssignedCount = newAssignments.Count;
            response.Message = $"Successfully auto-assigned survey to {newAssignments.Count} relationship(s)";

            if (skippedDuplicates > 0)
            {
                response.Message += $" ({skippedDuplicates} already assigned)";
            }

            _logger.LogInformation("Auto-assign completed: {AssignedCount} new assignments created for survey {SurveyId}, {SkippedCount} duplicates skipped",
                newAssignments.Count, surveyId, skippedDuplicates);

            return response;
        }
        catch (DbUpdateException dbEx)
        {
            // Edge case: Partial assignment failure (e.g., constraint violation)
            _logger.LogError(dbEx, "Database error during auto-assign for survey {SurveyId}", surveyId);
            response.Success = false;
            response.Message = "Database error occurred during auto-assignment. Some assignments may have failed.";
            response.Errors.Add("Database constraint violation - possible duplicate or invalid relationship");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-assign for survey {SurveyId}", surveyId);
            response.Success = false;
            response.Message = "An error occurred during auto-assignment";
            response.Errors.Add(ex.Message);
            return response;
        }
    }

    /// <summary>
    /// Calculate the number of questions in a survey schema
    /// </summary>
    private int CalculateQuestionCount(JsonDocument schema)
    {
        try
        {
            var root = schema.RootElement;

            if (root.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
            {
                int count = 0;
                foreach (var page in pages.EnumerateArray())
                {
                    if (page.TryGetProperty("elements", out var elements) && elements.ValueKind == JsonValueKind.Array)
                    {
                        count += elements.GetArrayLength();
                    }
                }
                return count;
            }
            else if (root.TryGetProperty("elements", out var elements) && elements.ValueKind == JsonValueKind.Array)
            {
                return elements.GetArrayLength();
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}

