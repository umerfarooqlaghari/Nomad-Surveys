using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nomad.Api.Services;

/// <summary>
/// Service for managing report templates and generating reports
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly NomadSurveysDbContext _context;
    private readonly IReportingService _reportingService;
    private readonly IPlaceholderReplacementService _placeholderService;
    private readonly IPdfGenerationService _pdfService;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        NomadSurveysDbContext context,
        IReportingService reportingService,
        IPlaceholderReplacementService placeholderService,
        IPdfGenerationService pdfService,
        ILogger<TemplateService> logger)
    {
        _context = context;
        _reportingService = reportingService;
        _placeholderService = placeholderService;
        _pdfService = pdfService;
        _logger = logger;
        
        // Set QuestPDF license (use free license in development)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<TemplateResponse?> GetTemplateByIdAsync(Guid templateId, Guid tenantId)
    {
        try
        {
            var template = await _context.ReportTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId);

            if (template == null)
            {
                return null;
            }

            return new TemplateResponse
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                TemplateSchema = template.TemplateSchema,
                PlaceholderMappings = template.PlaceholderMappings,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                TenantId = template.TenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<List<TemplateListResponse>> GetTemplatesAsync(Guid tenantId, bool? isActive = null)
    {
        try
        {
            var query = _context.ReportTemplates
                .Where(t => t.TenantId == tenantId);

            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            var templates = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return templates.Select(t => new TemplateListResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TemplateResponse> CreateTemplateAsync(CreateTemplateRequest request, Guid tenantId)
    {
        try
        {
            var template = new ReportTemplate
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                TemplateSchema = request.TemplateSchema,
                PlaceholderMappings = request.PlaceholderMappings,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.ReportTemplates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template created successfully: {TemplateId} for tenant {TenantId}", template.Id, tenantId);

            return new TemplateResponse
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                TemplateSchema = template.TemplateSchema,
                PlaceholderMappings = template.PlaceholderMappings,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                TenantId = template.TenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TemplateResponse?> UpdateTemplateAsync(Guid templateId, UpdateTemplateRequest request, Guid tenantId)
    {
        try
        {
            var template = await _context.ReportTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId);

            if (template == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                template.Name = request.Name;
            }

            if (request.Description != null)
            {
                template.Description = request.Description;
            }

            if (request.TemplateSchema != null)
            {
                template.TemplateSchema = request.TemplateSchema;
            }

            if (request.PlaceholderMappings != null)
            {
                template.PlaceholderMappings = request.PlaceholderMappings;
            }

            if (request.IsActive.HasValue)
            {
                template.IsActive = request.IsActive.Value;
            }

            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template updated successfully: {TemplateId}", templateId);

            return new TemplateResponse
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                TemplateSchema = template.TemplateSchema,
                PlaceholderMappings = template.PlaceholderMappings,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                TenantId = template.TenantId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId, Guid tenantId)
    {
        try
        {
            var template = await _context.ReportTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tenantId);

            if (template == null)
            {
                return false;
            }

            _context.ReportTemplates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template deleted successfully: {TemplateId}", templateId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<GeneratedReportResponse> GenerateReportAsync(GenerateReportRequest request, Guid tenantId)
    {
        try
        {
            // Get template
            var template = await GetTemplateByIdAsync(request.TemplateId, tenantId);
            if (template == null)
            {
                throw new InvalidOperationException($"Template {request.TemplateId} not found");
            }

            // Fetch report data using ReportingService
            var comprehensiveReport = await _reportingService.GetComprehensiveReportAsync(
                request.SubjectId,
                request.SurveyId,
                tenantId);

            if (comprehensiveReport == null)
            {
                throw new InvalidOperationException($"No report data found for subject {request.SubjectId}");
            }

            // Replace placeholders in template
            var processedTemplate = await _placeholderService.ReplacePlaceholdersAsync(
                template.TemplateSchema,
                comprehensiveReport,
                request.AdditionalData ?? new Dictionary<string, object>());

            // Generate PDF from processed template
            var pdfBytes = await _pdfService.GeneratePdfAsync(processedTemplate, tenantId);

            return new GeneratedReportResponse
            {
                PdfContent = pdfBytes,
                ContentType = "application/pdf",
                FileName = $"report_{request.SubjectId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for template {TemplateId} and subject {SubjectId}", 
                request.TemplateId, request.SubjectId);
            throw;
        }
    }
}


