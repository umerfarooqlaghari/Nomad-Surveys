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

