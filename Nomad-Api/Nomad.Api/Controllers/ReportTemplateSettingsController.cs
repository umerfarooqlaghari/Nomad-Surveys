using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/report-template-settings")]
[AuthorizeTenant]
public class ReportTemplateSettingsController : ControllerBase
{
    private readonly IReportTemplateSettingsService _templateSettingsService;
    private readonly ILogger<ReportTemplateSettingsController> _logger;

    public ReportTemplateSettingsController(
        IReportTemplateSettingsService templateSettingsService,
        ILogger<ReportTemplateSettingsController> logger)
    {
        _templateSettingsService = templateSettingsService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all template settings for the tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ReportTemplateSettingsResponse>>> GetTemplateSettings([FromQuery] bool? isActive = null)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var templates = await _templateSettingsService.GetTemplateSettingsAsync(tenantId.Value, isActive);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template settings");
            return StatusCode(500, new { message = "An error occurred while retrieving template settings" });
        }
    }

    /// <summary>
    /// Get default template settings
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<ReportTemplateSettingsResponse>> GetDefaultTemplateSettings()
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var template = await _templateSettingsService.GetDefaultTemplateSettingsAsync(tenantId.Value);
            if (template == null)
            {
                return NotFound(new { message = "No default template settings found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default template settings");
            return StatusCode(500, new { message = "An error occurred while retrieving default template settings" });
        }
    }

    /// <summary>
    /// Get template settings by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ReportTemplateSettingsResponse>> GetTemplateSettingsById(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var template = await _templateSettingsService.GetTemplateSettingsByIdAsync(id, tenantId.Value);
            if (template == null)
            {
                return NotFound(new { message = "Template settings not found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template settings");
            return StatusCode(500, new { message = "An error occurred while retrieving template settings" });
        }
    }

    /// <summary>
    /// Create new template settings
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReportTemplateSettingsResponse>> CreateTemplateSettings([FromBody] CreateReportTemplateSettingsRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var template = await _templateSettingsService.CreateTemplateSettingsAsync(request, tenantId.Value);
            return CreatedAtAction(nameof(GetTemplateSettingsById), new { id = template.Id }, template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template settings");
            return StatusCode(500, new { message = "An error occurred while creating template settings" });
        }
    }

    /// <summary>
    /// Update template settings
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ReportTemplateSettingsResponse>> UpdateTemplateSettings(Guid id, [FromBody] UpdateReportTemplateSettingsRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var template = await _templateSettingsService.UpdateTemplateSettingsAsync(id, request, tenantId.Value);
            if (template == null)
            {
                return NotFound(new { message = "Template settings not found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template settings");
            return StatusCode(500, new { message = "An error occurred while updating template settings" });
        }
    }

    /// <summary>
    /// Delete template settings
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTemplateSettings(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var deleted = await _templateSettingsService.DeleteTemplateSettingsAsync(id, tenantId.Value);
            if (!deleted)
            {
                return NotFound(new { message = "Template settings not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template settings");
            return StatusCode(500, new { message = "An error occurred while deleting template settings" });
        }
    }

    /// <summary>
    /// Set template settings as default
    /// </summary>
    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetAsDefault(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var success = await _templateSettingsService.SetAsDefaultAsync(id, tenantId.Value);
            if (!success)
            {
                return NotFound(new { message = "Template settings not found or not active" });
            }

            return Ok(new { message = "Template settings set as default" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting template as default");
            return StatusCode(500, new { message = "An error occurred while setting template as default" });
        }
    }

    /// <summary>
    /// Save template settings with image uploads (multipart form data)
    /// </summary>
    [HttpPost("save")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<ReportTemplateSettingsResponse>> SaveTemplateSettings()
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized("Tenant ID not found");
            }

            var form = await Request.ReadFormAsync();
            
            _logger.LogInformation("Received save template request. Form has {FieldCount} fields and {FileCount} files", 
                form.Count, form.Files?.Count ?? 0);
            
            // Extract form fields
            var name = form["name"].FirstOrDefault() ?? "Default Template";
            var description = form["description"].FirstOrDefault();
            var companyName = form["companyName"].FirstOrDefault();
            var primaryColor = form["primaryColor"].FirstOrDefault();
            var secondaryColor = form["secondaryColor"].FirstOrDefault();
            var tertiaryColor = form["tertiaryColor"].FirstOrDefault();
            var isDefault = bool.TryParse(form["isDefault"].FirstOrDefault(), out var defaultVal) && defaultVal;

            _logger.LogInformation("Template settings: Name={Name}, CompanyName={CompanyName}, Colors: Primary={Primary}, Secondary={Secondary}, Tertiary={Tertiary}",
                name, companyName, primaryColor, secondaryColor, tertiaryColor);

            // Get image URLs from form (images should be uploaded to library first via CloudinaryController)
            var companyLogoUrl = form["companyLogoUrl"].FirstOrDefault();
            var coverImageUrl = form["coverImageUrl"].FirstOrDefault();
            
            _logger.LogInformation("Image URLs - Logo: {LogoUrl}, Cover: {CoverUrl}", 
                companyLogoUrl ?? "none", coverImageUrl ?? "none");

            // Log final URLs before saving
            _logger.LogInformation("Final URLs to save - Logo: {LogoUrl}, Cover: {CoverUrl}", 
                companyLogoUrl ?? "null", coverImageUrl ?? "null");

            // Check if there's an existing default template to update, or create new
            var existingDefault = await _templateSettingsService.GetDefaultTemplateSettingsAsync(tenantId.Value);
            
            if (existingDefault != null)
            {
                // Use new URLs if uploaded, otherwise keep existing, or use passed existing URLs
                var finalLogoUrl = !string.IsNullOrEmpty(companyLogoUrl)
                    ? companyLogoUrl
                    : existingDefault.CompanyLogoUrl; // Removed reference to non-existent 'existingLogoUrl'
                        
var finalCoverUrl = !string.IsNullOrEmpty(coverImageUrl)
    ? coverImageUrl
    : existingDefault.CoverImageUrl; // Removed reference to non-existent 'existingCoverUrl'
                
                _logger.LogInformation("Updating existing template. Final URLs - Logo: {LogoUrl}, Cover: {CoverUrl}", 
                    finalLogoUrl ?? "null", finalCoverUrl ?? "null");
                
                var updateRequest = new UpdateReportTemplateSettingsRequest
                {
                    Name = name,
                    Description = description,
                    CompanyName = companyName,
                    // Always set URLs (even if null) to ensure they're updated in database
                    CompanyLogoUrl = finalLogoUrl,
                    CoverImageUrl = finalCoverUrl,
                    PrimaryColor = primaryColor,
                    SecondaryColor = secondaryColor,
                    TertiaryColor = tertiaryColor,
                    IsDefault = true
                };

                var updated = await _templateSettingsService.UpdateTemplateSettingsAsync(existingDefault.Id, updateRequest, tenantId.Value);
                if (updated != null)
                {
                    _logger.LogInformation("Updated existing template settings {TemplateId} for tenant {TenantId}. Saved URLs - Logo: {LogoUrl}, Cover: {CoverUrl}", 
                        existingDefault.Id, tenantId.Value, updated.CompanyLogoUrl ?? "null", updated.CoverImageUrl ?? "null");
                    return Ok(updated);
                }
            }

            // Create new template settings
            _logger.LogInformation("Creating new template. URLs - Logo: {LogoUrl}, Cover: {CoverUrl}", 
                companyLogoUrl ?? "null", coverImageUrl ?? "null");
            
            var createRequest = new CreateReportTemplateSettingsRequest
            {
                Name = name,
                Description = description,
                CompanyName = companyName,
                CompanyLogoUrl = companyLogoUrl,
                CoverImageUrl = coverImageUrl,
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                TertiaryColor = tertiaryColor,
                IsDefault = isDefault || existingDefault == null // Set as default if no default exists
            };

            var created = await _templateSettingsService.CreateTemplateSettingsAsync(createRequest, tenantId.Value);
            _logger.LogInformation("Created new template settings {TemplateId} for tenant {TenantId}. Saved URLs - Logo: {LogoUrl}, Cover: {CoverUrl}", 
                created.Id, tenantId.Value, created.CompanyLogoUrl ?? "null", created.CoverImageUrl ?? "null");
            
            return CreatedAtAction(nameof(GetTemplateSettingsById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving template settings");
            return StatusCode(500, new { message = "An error occurred while saving template settings", error = ex.Message });
        }
    }
}


