using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ICompetencyService _competencyService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        IQuestionService questionService,
        ICompetencyService competencyService,
        ILogger<QuestionsController> logger)
    {
        _questionService = questionService;
        _competencyService = competencyService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all questions for the current tenant, optionally filtered by competency
    /// </summary>
    /// <param name="competencyId">Optional competency ID to filter by</param>
    /// <returns>List of questions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<QuestionListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<QuestionListResponse>>> GetQuestions([FromQuery] Guid? competencyId = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var questions = await _questionService.GetQuestionsAsync(tenantId, competencyId);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving questions");
            return StatusCode(500, new { error = "An error occurred while retrieving questions" });
        }
    }

    /// <summary>
    /// Get a specific question by ID
    /// </summary>
    /// <param name="id">Question ID</param>
    /// <returns>Question details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuestionResponse>> GetQuestionById(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var question = await _questionService.GetQuestionByIdAsync(id);
            
            if (question == null)
            {
                return NotFound(new { error = $"Question with ID {id} not found" });
            }

            // Verify the question belongs to the current tenant
            if (question.TenantId != tenantId)
            {
                return NotFound(new { error = $"Question with ID {id} not found" });
            }

            return Ok(question);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the question" });
        }
    }

    /// <summary>
    /// Create a new question
    /// </summary>
    /// <param name="request">Question creation request</param>
    /// <returns>Created question</returns>
    [HttpPost]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuestionResponse>> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify competency exists and belongs to tenant
            var competencyExists = await _competencyService.CompetencyExistsAsync(request.CompetencyId, tenantId);
            if (!competencyExists)
            {
                return BadRequest(new { error = $"Competency with ID {request.CompetencyId} not found or does not belong to your tenant" });
            }

            var question = await _questionService.CreateQuestionAsync(request, tenantId.Value);

            // Return 201 Created with the created question
            // Note: CreatedAtAction fails with tenant-scoped routes, so we return Created with a simple status
            return StatusCode(StatusCodes.Status201Created, question);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating question");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question");
            return StatusCode(500, new { error = "An error occurred while creating the question" });
        }
    }

    /// <summary>
    /// Update an existing question
    /// </summary>
    /// <param name="id">Question ID</param>
    /// <param name="request">Question update request</param>
    /// <returns>Updated question</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuestionResponse>> UpdateQuestion(Guid id, [FromBody] UpdateQuestionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify question exists and belongs to tenant
            var existingQuestion = await _questionService.GetQuestionByIdAsync(id);
            if (existingQuestion == null || existingQuestion.TenantId != tenantId)
            {
                return NotFound(new { error = $"Question with ID {id} not found" });
            }

            // Verify competency exists and belongs to tenant
            var competencyExists = await _competencyService.CompetencyExistsAsync(request.CompetencyId, tenantId);
            if (!competencyExists)
            {
                return BadRequest(new { error = $"Competency with ID {request.CompetencyId} not found or does not belong to your tenant" });
            }

            var question = await _questionService.UpdateQuestionAsync(id, request);
            
            if (question == null)
            {
                return NotFound(new { error = $"Question with ID {id} not found" });
            }

            return Ok(question);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating question {QuestionId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the question" });
        }
    }

    /// <summary>
    /// Delete a question (soft delete)
    /// </summary>
    /// <param name="id">Question ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify question exists and belongs to tenant
            var existingQuestion = await _questionService.GetQuestionByIdAsync(id);
            if (existingQuestion == null || existingQuestion.TenantId != tenantId)
            {
                return NotFound(new { error = $"Question with ID {id} not found" });
            }

            var result = await _questionService.DeleteQuestionAsync(id);
            
            if (!result)
            {
                return NotFound(new { error = $"Question with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the question" });
        }
    }
    /// <summary>
    /// Upload a question bank from an Excel file
    /// </summary>
    /// <param name="file">The Excel file containing clusters, competencies, and questions</param>
    /// <returns>Success status</returns>
    [HttpPost("upload-question-bank")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadQuestionBank(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
            {
                return BadRequest(new { error = "Only .xlsx files are supported" });
            }

            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            using var stream = file.OpenReadStream();
            var success = await _questionService.UploadQuestionBankAsync(tenantId.Value, stream);

            if (!success)
            {
                return BadRequest(new { error = "Failed to process the question bank file" });
            }

            return Ok(new { message = "Question bank uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading question bank");
            return StatusCode(500, new { error = "An error occurred while uploading the question bank: " + ex.Message });
        }
    }

    /// <summary>
    /// Download a template for the question bank upload
    /// </summary>
    /// <returns>Excel file template</returns>
    [HttpGet("download-template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadTemplate()
    {
        try
        {
            var (content, fileName) = await _questionService.GenerateQuestionBankTemplateAsync();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating question bank template");
            return StatusCode(500, new { error = "An error occurred while generating the template" });
        }
    }
}

