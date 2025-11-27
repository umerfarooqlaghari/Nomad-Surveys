using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Nomad.Api.Services;

namespace Nomad.Api.Controllers;

[ApiController]
public class ReportTemplateController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReportTemplateController> _logger;
    private readonly ICloudinaryService _cloudinary;
    private readonly ReportTemplateService _templateService;

    public ReportTemplateController(
        IWebHostEnvironment env,
        ILogger<ReportTemplateController> logger,
        ICloudinaryService cloudinary,
        ReportTemplateService templateService)
    {
        _env = env;
        _logger = logger;
        _cloudinary = cloudinary;
        _templateService = templateService;
    }

    [HttpGet("/api/{tenant}/report-template")]
    public async Task<IActionResult> GetTemplate(string tenant)
    {
        try
        {
            // Prefer tenant-specific template if available
            var tenantPath = Path.Combine(_env.ContentRootPath, "Templates", tenant, "ReportTemplate.html");
            var defaultPath = Path.Combine(_env.ContentRootPath, "Templates", "ReportTemplate.html");

            string pathToUse = System.IO.File.Exists(tenantPath) ? tenantPath : defaultPath;
            if (!System.IO.File.Exists(pathToUse)) return NotFound(new { error = "Template not found" });

            var html = await System.IO.File.ReadAllTextAsync(pathToUse);

            // Return raw HTML so frontend can render/edit it directly
            return Content(html, "text/html");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to read template");
            return StatusCode(500, new { error = "Failed to read template" });
        }
    }

    [HttpPost("/api/{tenant}/report-template")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SaveTemplate([FromRoute] string tenant)
    {
        try
        {
            var form = await Request.ReadFormAsync();
            var html = form["html"].FirstOrDefault();

            string? logoUrl = null;
            string? coverUrl = null;

            if (form.Files != null && form.Files.Count > 0)
            {
                foreach (var file in form.Files)
                {
                    if (file.Name == "companyLogo")
                    {
                        var uploadResult = await _cloudinary.UploadImageAsync(file, "report_templates/logos");
                        logoUrl = uploadResult.url; // Extract the URL from the tuple
                    }
                    else if (file.Name == "coverImage")
                    {
                        var uploadResult = await _cloudinary.UploadImageAsync(file, "report_templates/covers");
                        coverUrl = uploadResult.url; // Extract the URL from the tuple
                    }
                }
            }

            if (!string.IsNullOrEmpty(html))
            {
                // Save tenant-specific template if tenant provided
                await _templateService.SaveTemplateHtmlAsync(html, tenant);
            }

            return Ok(new { success = true, logoUrl, coverUrl });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to save template");
            return StatusCode(500, new { error = "Failed to save template" });
        }
    }
}
