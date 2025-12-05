using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.Services;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("api/admin/cloudinary")]
[AuthorizeSuperAdmin]
public class AdminCloudinaryController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<AdminCloudinaryController> _logger;

    public AdminCloudinaryController(ICloudinaryService cloudinaryService, ILogger<AdminCloudinaryController> logger)
    {
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single image (SuperAdmin only)
    /// </summary>
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadImage(IFormFile image, [FromQuery] string? folder = null)
    {
        try
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { message = "No image file provided" });
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
            {
                return BadRequest(new { message = "Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed." });
            }

            // Validate file size (max 20MB)
            if (image.Length > 20 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size too large. Maximum size is 20MB." });
            }

            var (url, error) = await _cloudinaryService.UploadImageAsync(image, folder);
            
            if (!string.IsNullOrEmpty(url))
            {
                return Ok(new { imageUrl = url, url = url, message = "Image uploaded successfully" });
            }
            else
            {
                return StatusCode(500, new { message = "Failed to upload image", error = error ?? "Unknown error" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return StatusCode(500, new { message = "Failed to upload image", error = ex.Message });
        }
    }

    /// <summary>
    /// Upload multiple images (bulk upload) (SuperAdmin only)
    /// </summary>
    [HttpPost("upload/bulk")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadImages(IFormFileCollection images, [FromQuery] string? folder = null)
    {
        try
        {
            if (images == null || images.Count == 0)
            {
                return BadRequest(new { message = "No image files provided" });
            }

            // Validate file types and sizes
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            foreach (var image in images)
            {
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    return BadRequest(new { message = $"Invalid file type for {image.FileName}. Only JPEG, PNG, GIF, and WebP are allowed." });
                }

                if (image.Length > 20 * 1024 * 1024)
                {
                    return BadRequest(new { message = $"File {image.FileName} is too large. Maximum size is 20MB." });
                }
            }

            var results = await _cloudinaryService.UploadImagesAsync(images, folder);
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            return Ok(new
            {
                results,
                summary = new
                {
                    totalFiles = images.Count,
                    successCount,
                    failureCount
                },
                message = $"Bulk upload completed. {successCount} successful, {failureCount} failed."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images");
            return StatusCode(500, new { message = "Failed to upload images", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all images from the library (SuperAdmin only)
    /// </summary>
    [HttpGet("images")]
    public async Task<IActionResult> GetAllImages([FromQuery] string? folder = null)
    {
        try
        {
            var images = await _cloudinaryService.GetAllImagesAsync(folder);
            return Ok(new { images, message = "Images retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving images");
            return StatusCode(500, new { message = "Failed to retrieve images", error = ex.Message });
        }
    }
}

