using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.Models;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(NomadSurveysDbContext context, ILogger<SurveysController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Survey>>> GetSurveys()
    {
        try
        {
            var surveys = await _context.Surveys.ToListAsync();
            return Ok(surveys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving surveys");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Survey>> GetSurvey(int id)
    {
        try
        {
            var survey = await _context.Surveys.FindAsync(id);
            
            if (survey == null)
            {
                return NotFound();
            }

            return Ok(survey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving survey with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Survey>> CreateSurvey(Survey survey)
    {
        try
        {
            survey.CreatedAt = DateTime.UtcNow;
            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, survey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating survey");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSurvey(int id, Survey survey)
    {
        if (id != survey.Id)
        {
            return BadRequest();
        }

        try
        {
            survey.UpdatedAt = DateTime.UtcNow;
            _context.Entry(survey).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await SurveyExists(id))
            {
                return NotFound();
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating survey with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSurvey(int id)
    {
        try
        {
            var survey = await _context.Surveys.FindAsync(id);
            if (survey == null)
            {
                return NotFound();
            }

            _context.Surveys.Remove(survey);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting survey with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<bool> SurveyExists(int id)
    {
        return await _context.Surveys.AnyAsync(e => e.Id == id);
    }
}
