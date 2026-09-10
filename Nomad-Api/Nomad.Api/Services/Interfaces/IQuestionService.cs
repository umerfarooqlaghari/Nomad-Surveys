using Alpha.Api.DTOs.Request;
using Alpha.Api.DTOs.Response;

namespace Alpha.Api.Services.Interfaces;

public interface IQuestionService
{
    Task<List<QuestionListResponse>> GetQuestionsAsync(Guid? tenantId = null, Guid? competencyId = null);
    Task<QuestionResponse?> GetQuestionByIdAsync(Guid questionId);
    Task<QuestionResponse> CreateQuestionAsync(CreateQuestionRequest request, Guid tenantId);
    Task<QuestionResponse?> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request);
    Task<bool> DeleteQuestionAsync(Guid questionId);
    Task<bool> QuestionExistsAsync(Guid questionId, Guid? tenantId = null);
    
    // Bulk Operations
    Task<bool> UploadQuestionBankAsync(Guid tenantId, Stream excelStream);
    Task<(byte[] FileContent, string FileName)> GenerateQuestionBankTemplateAsync();
}

