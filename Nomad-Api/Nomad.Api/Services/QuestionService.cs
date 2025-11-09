using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class QuestionService : IQuestionService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(NomadSurveysDbContext context, ILogger<QuestionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<QuestionListResponse>> GetQuestionsAsync(Guid? tenantId = null, Guid? competencyId = null)
    {
        try
        {
            var query = _context.Questions
                .Include(q => q.Competency)
                .Where(q => q.IsActive) // Only return active (non-deleted) questions
                .AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(q => q.TenantId == tenantId.Value);
            }

            if (competencyId.HasValue)
            {
                query = query.Where(q => q.CompetencyId == competencyId.Value);
            }

            var questions = await query
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return questions.Select(q => new QuestionListResponse
            {
                Id = q.Id,
                CompetencyId = q.CompetencyId,
                CompetencyName = q.Competency?.Name,
                SelfQuestion = q.SelfQuestion,
                OthersQuestion = q.OthersQuestion,
                IsActive = q.IsActive,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving questions for tenant {TenantId}, competency {CompetencyId}", tenantId, competencyId);
            throw;
        }
    }

    public async Task<QuestionResponse?> GetQuestionByIdAsync(Guid questionId)
    {
        try
        {
            var question = await _context.Questions
                .Include(q => q.Competency)
                .FirstOrDefaultAsync(q => q.Id == questionId && q.IsActive); // Only return active questions

            if (question == null)
            {
                return null;
            }

            return new QuestionResponse
            {
                Id = question.Id,
                CompetencyId = question.CompetencyId,
                CompetencyName = question.Competency?.Name,
                SelfQuestion = question.SelfQuestion,
                OthersQuestion = question.OthersQuestion,
                IsActive = question.IsActive,
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt,
                TenantId = question.TenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving question {QuestionId}", questionId);
            throw;
        }
    }

    public async Task<QuestionResponse> CreateQuestionAsync(CreateQuestionRequest request, Guid tenantId)
    {
        try
        {
            // Verify competency exists and belongs to the same tenant
            var competency = await _context.Competencies
                .FirstOrDefaultAsync(c => c.Id == request.CompetencyId && c.TenantId == tenantId);

            if (competency == null)
            {
                throw new InvalidOperationException($"Competency {request.CompetencyId} not found or does not belong to tenant {tenantId}");
            }

            var question = new Question
            {
                Id = Guid.NewGuid(),
                CompetencyId = request.CompetencyId,
                SelfQuestion = request.SelfQuestion,
                OthersQuestion = request.OthersQuestion,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created question {QuestionId} for competency {CompetencyId}, tenant {TenantId}", 
                question.Id, request.CompetencyId, tenantId);

            return new QuestionResponse
            {
                Id = question.Id,
                CompetencyId = question.CompetencyId,
                CompetencyName = competency.Name,
                SelfQuestion = question.SelfQuestion,
                OthersQuestion = question.OthersQuestion,
                IsActive = question.IsActive,
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt,
                TenantId = question.TenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<QuestionResponse?> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request)
    {
        try
        {
            var question = await _context.Questions
                .Include(q => q.Tenant)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return null;
            }

            // Verify competency exists and belongs to the same tenant
            var competency = await _context.Competencies
                .FirstOrDefaultAsync(c => c.Id == request.CompetencyId && c.TenantId == question.TenantId);

            if (competency == null)
            {
                throw new InvalidOperationException($"Competency {request.CompetencyId} not found or does not belong to the same tenant");
            }

            question.CompetencyId = request.CompetencyId;
            question.SelfQuestion = request.SelfQuestion;
            question.OthersQuestion = request.OthersQuestion;
            question.UpdatedAt = DateTime.UtcNow;

            if (request.IsActive.HasValue)
            {
                question.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated question {QuestionId}", questionId);

            return await GetQuestionByIdAsync(questionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", questionId);
            throw;
        }
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId)
    {
        try
        {
            var question = await _context.Questions.FindAsync(questionId);

            if (question == null)
            {
                return false;
            }

            // Soft delete
            question.IsActive = false;
            question.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted (soft) question {QuestionId}", questionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
            throw;
        }
    }

    public async Task<bool> QuestionExistsAsync(Guid questionId, Guid? tenantId = null)
    {
        try
        {
            var query = _context.Questions
                .Where(q => q.Id == questionId && q.IsActive); // Only check active questions

            if (tenantId.HasValue)
            {
                query = query.Where(q => q.TenantId == tenantId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if question {QuestionId} exists", questionId);
            throw;
        }
    }
}

