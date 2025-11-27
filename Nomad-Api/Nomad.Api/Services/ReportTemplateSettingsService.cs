using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class ReportTemplateSettingsService : IReportTemplateSettingsService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<ReportTemplateSettingsService> _logger;

    public ReportTemplateSettingsService(
        NomadSurveysDbContext context,
        ILogger<ReportTemplateSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ReportTemplateSettingsResponse>> GetTemplateSettingsAsync(Guid tenantId, bool? isActive = null)
    {
        var query = _context.ReportTemplateSettings
            .Where(ts => ts.TenantId == tenantId);

        if (isActive.HasValue)
        {
            query = query.Where(ts => ts.IsActive == isActive.Value);
        }

        var templates = await query
            .OrderByDescending(ts => ts.CreatedAt)
            .ToListAsync();

        return templates.Select(MapToResponse).ToList();
    }

    public async Task<ReportTemplateSettingsResponse?> GetTemplateSettingsByIdAsync(Guid id, Guid tenantId)
    {
        var template = await _context.ReportTemplateSettings
            .FirstOrDefaultAsync(ts => ts.Id == id && ts.TenantId == tenantId);

        return template != null ? MapToResponse(template) : null;
    }

    public async Task<ReportTemplateSettingsResponse?> GetDefaultTemplateSettingsAsync(Guid tenantId)
    {
        var template = await _context.ReportTemplateSettings
            .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.IsDefault && ts.IsActive);

        return template != null ? MapToResponse(template) : null;
    }

    public async Task<ReportTemplateSettingsResponse> CreateTemplateSettingsAsync(
        CreateReportTemplateSettingsRequest request,
        Guid tenantId)
    {
        // If this is set as default, unset other defaults
        if (request.IsDefault)
        {
            var existingDefaults = await _context.ReportTemplateSettings
                .Where(ts => ts.TenantId == tenantId && ts.IsDefault)
                .ToListAsync();

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        var template = new ReportTemplateSettings
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CompanyName = request.CompanyName,
            CompanyLogoUrl = request.CompanyLogoUrl,
            CoverImageUrl = request.CoverImageUrl,
            PrimaryColor = request.PrimaryColor,
            SecondaryColor = request.SecondaryColor,
            TertiaryColor = request.TertiaryColor,
            IsActive = true,
            IsDefault = request.IsDefault,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReportTemplateSettings.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created report template settings {TemplateId} for tenant {TenantId}", template.Id, tenantId);

        return MapToResponse(template);
    }

    public async Task<ReportTemplateSettingsResponse?> UpdateTemplateSettingsAsync(
        Guid id,
        UpdateReportTemplateSettingsRequest request,
        Guid tenantId)
    {
        var template = await _context.ReportTemplateSettings
            .FirstOrDefaultAsync(ts => ts.Id == id && ts.TenantId == tenantId);

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

        if (request.CompanyName != null)
        {
            template.CompanyName = request.CompanyName;
        }

        // Update URLs - always update when provided (even if null/empty to clear)
        // The controller always sets these values, so we should always update them
        template.CompanyLogoUrl = request.CompanyLogoUrl;
        template.CoverImageUrl = request.CoverImageUrl;
        
        _logger.LogInformation("Setting URLs - CompanyLogoUrl: {LogoUrl}, CoverImageUrl: {CoverUrl}", 
            request.CompanyLogoUrl ?? "null", request.CoverImageUrl ?? "null");

        if (request.PrimaryColor != null)
        {
            template.PrimaryColor = request.PrimaryColor;
        }

        if (request.SecondaryColor != null)
        {
            template.SecondaryColor = request.SecondaryColor;
        }

        if (request.TertiaryColor != null)
        {
            template.TertiaryColor = request.TertiaryColor;
        }

        if (request.IsActive.HasValue)
        {
            template.IsActive = request.IsActive.Value;
        }

        if (request.IsDefault.HasValue && request.IsDefault.Value)
        {
            // Unset other defaults
            var existingDefaults = await _context.ReportTemplateSettings
                .Where(ts => ts.TenantId == tenantId && ts.IsDefault && ts.Id != id)
                .ToListAsync();

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }

            template.IsDefault = true;
        }

        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated report template settings {TemplateId} for tenant {TenantId}", id, tenantId);

        return MapToResponse(template);
    }

    public async Task<bool> DeleteTemplateSettingsAsync(Guid id, Guid tenantId)
    {
        var template = await _context.ReportTemplateSettings
            .FirstOrDefaultAsync(ts => ts.Id == id && ts.TenantId == tenantId);

        if (template == null)
        {
            return false;
        }

        _context.ReportTemplateSettings.Remove(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted report template settings {TemplateId} for tenant {TenantId}", id, tenantId);

        return true;
    }

    public async Task<bool> SetAsDefaultAsync(Guid id, Guid tenantId)
    {
        var template = await _context.ReportTemplateSettings
            .FirstOrDefaultAsync(ts => ts.Id == id && ts.TenantId == tenantId);

        if (template == null || !template.IsActive)
        {
            return false;
        }

        // Unset other defaults
        var existingDefaults = await _context.ReportTemplateSettings
            .Where(ts => ts.TenantId == tenantId && ts.IsDefault && ts.Id != id)
            .ToListAsync();

        foreach (var existing in existingDefaults)
        {
            existing.IsDefault = false;
        }

        template.IsDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Set report template settings {TemplateId} as default for tenant {TenantId}", id, tenantId);

        return true;
    }

    private static ReportTemplateSettingsResponse MapToResponse(ReportTemplateSettings template)
    {
        return new ReportTemplateSettingsResponse
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            CompanyName = template.CompanyName,
            // Ensure URLs are always included in response (use empty string instead of null to ensure they appear in JSON)
            CompanyLogoUrl = template.CompanyLogoUrl ?? string.Empty,
            CoverImageUrl = template.CoverImageUrl ?? string.Empty,
            PrimaryColor = template.PrimaryColor,
            SecondaryColor = template.SecondaryColor,
            TertiaryColor = template.TertiaryColor,
            IsActive = template.IsActive,
            IsDefault = template.IsDefault,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}


