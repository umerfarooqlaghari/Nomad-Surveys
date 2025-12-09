using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Common;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using System.Text.Json;

namespace Nomad.Api.Services;

public class TenantSettingsService : ITenantSettingsService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<TenantSettingsService> _logger;

    public TenantSettingsService(NomadSurveysDbContext context, ILogger<TenantSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TenantSettingsResponse?> GetSettingsByTenantIdAsync(Guid tenantId)
    {
        try
        {
            var settings = await _context.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (settings == null)
            {
                return null;
            }

            return MapToResponse(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant settings for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TenantSettingsResponse> CreateSettingsAsync(CreateTenantSettingsRequest request, Guid tenantId)
    {
        try
        {
            // Check if settings already exist for this tenant
            var existingSettings = await _context.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (existingSettings != null)
            {
                throw new InvalidOperationException($"Settings already exist for tenant {tenantId}");
            }

            // Serialize rating options to JsonDocument
            JsonDocument? ratingOptionsJson = null;
            if (request.DefaultRatingOptions != null && request.DefaultRatingOptions.Any())
            {
                var json = JsonSerializer.Serialize(request.DefaultRatingOptions);
                ratingOptionsJson = JsonDocument.Parse(json);
            }

            var settings = new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                DefaultQuestionType = request.DefaultQuestionType,
                DefaultRatingOptions = ratingOptionsJson,
                NumberOfOptions = request.NumberOfOptions,
                CreatedAt = DateTime.UtcNow,
            };

            _context.TenantSettings.Add(settings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tenant settings created successfully: {SettingsId} for tenant {TenantId}", settings.Id, tenantId);

            return MapToResponse(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant settings for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TenantSettingsResponse> UpdateSettingsAsync(UpdateTenantSettingsRequest request, Guid tenantId)
    {
        try
        {
            var settings = await _context.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (settings == null)
            {
                throw new InvalidOperationException($"Settings not found for tenant {tenantId}");
            }

            // Serialize rating options to JsonDocument
            JsonDocument? ratingOptionsJson = null;
            if (request.DefaultRatingOptions != null && request.DefaultRatingOptions.Any())
            {
                var json = JsonSerializer.Serialize(request.DefaultRatingOptions);
                ratingOptionsJson = JsonDocument.Parse(json);
            }

            settings.DefaultQuestionType = request.DefaultQuestionType;
            settings.DefaultRatingOptions = ratingOptionsJson;
            settings.NumberOfOptions = request.NumberOfOptions;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tenant settings updated successfully: {SettingsId} for tenant {TenantId}", settings.Id, tenantId);

            return MapToResponse(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant settings for tenant {TenantId}", tenantId);
            throw;
        }
    }

    private TenantSettingsResponse MapToResponse(TenantSettings settings)
    {
        List<RatingOptionDto>? ratingOptions = null;
        if (settings.DefaultRatingOptions != null)
        {
            var json = settings.DefaultRatingOptions.RootElement.GetRawText();
            ratingOptions = JsonSerializer.Deserialize<List<RatingOptionDto>>(json);
        }

        return new TenantSettingsResponse
        {
            Id = settings.Id,
            TenantId = settings.TenantId,
            DefaultQuestionType = settings.DefaultQuestionType,
            DefaultRatingOptions = ratingOptions,
            NumberOfOptions = settings.NumberOfOptions,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
    }
}

