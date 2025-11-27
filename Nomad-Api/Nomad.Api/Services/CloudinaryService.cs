using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nomad.Api.Services;

public interface ICloudinaryService
{
    Task<(string? url, string? error)> UploadImageAsync(IFormFile file, string? folder = null);
    Task<(string? url, string? error)> UploadImageFromBase64Async(string base64Data, string? folder = null, string? fileName = null);
    Task<List<CloudinaryImageInfo>> GetAllImagesAsync(string? folder = null);
    Task<bool> DeleteImageAsync(string publicId);
    Task<List<BulkUploadResult>> UploadImagesAsync(IFormFileCollection files, string? folder = null);
    Task<CloudinaryImageInfo?> GetImageAsync(string publicId);
}

public class CloudinaryService : ICloudinaryService
{
    private readonly ILogger<CloudinaryService> _logger;
    private readonly Cloudinary _cloudinary;
    private readonly string _defaultFolder;

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        
        var cloudName = config["NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME"] ?? config["CLOUDINARY_CLOUD_NAME"] ?? string.Empty;
        var apiKey = config["NEXT_PUBLIC_CLOUDINARY_API_KEY"] ?? config["CLOUDINARY_API_KEY"] ?? string.Empty;
        var apiSecret = config["CLOUDINARY_API_SECRET"] ?? string.Empty;
        _defaultFolder = config["CLOUDINARY_DEFAULT_FOLDER"] ?? "nomad-surveys";

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("Cloudinary configuration is incomplete. CloudName: {CloudName}, HasApiKey: {HasApiKey}, HasApiSecret: {HasApiSecret}",
                !string.IsNullOrEmpty(cloudName), !string.IsNullOrEmpty(apiKey), !string.IsNullOrEmpty(apiSecret));
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<(string? url, string? error)> UploadImageAsync(IFormFile file, string? folder = null)
    {
        if (file == null || file.Length == 0)
        {
            var error = "File is null or empty";
            _logger.LogWarning(error);
            return (null, error);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder ?? _defaultFolder,
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            
            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var secureUrl = uploadResult.SecureUrl.ToString();
                _logger.LogInformation("Successfully uploaded image to Cloudinary: {Url}", secureUrl);
                return (secureUrl, null);
            }
            else
            {
                var error = $"Upload failed with status: {uploadResult.StatusCode}";
                _logger.LogError(error);
                return (null, error);
            }
        }
        catch (Exception ex)
        {
            var error = $"Cloudinary upload error: {ex.Message}";
            _logger.LogError(ex, error);
            return (null, error);
        }
    }

    public async Task<(string? url, string? error)> UploadImageFromBase64Async(string base64Data, string? folder = null, string? fileName = null)
    {
        if (string.IsNullOrEmpty(base64Data))
        {
            var error = "Base64 data is null or empty";
            _logger.LogWarning(error);
            return (null, error);
        }

        try
        {
            // Parse base64 data URL
            string base64String;
            string extension = "jpg";

            if (base64Data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = base64Data.Split(',');
                if (parts.Length != 2)
                {
                    var error = "Invalid base64 data URL format";
                    _logger.LogError(error);
                    return (null, error);
                }

                var header = parts[0];
                base64String = parts[1];

                // Extract content type from header
                if (header.Contains("image/"))
                {
                    var contentTypeMatch = System.Text.RegularExpressions.Regex.Match(header, @"image/([^;]+)");
                    if (contentTypeMatch.Success)
                    {
                        var imageType = contentTypeMatch.Groups[1].Value.ToLowerInvariant();
                        extension = imageType switch
                        {
                            "png" => "png",
                            "gif" => "gif",
                            "webp" => "webp",
                            _ => "jpg"
                        };
                    }
                }
            }
            else
            {
                base64String = base64Data;
            }

            // Decode base64 to bytes
            byte[] fileBytes;
            try
            {
                fileBytes = Convert.FromBase64String(base64String);
            }
            catch (FormatException ex)
            {
                var error = $"Failed to decode base64 string: {ex.Message}";
                _logger.LogError(ex, error);
                return (null, error);
            }

            if (fileBytes.Length == 0)
            {
                var error = "Decoded base64 data is empty";
                _logger.LogWarning(error);
                return (null, error);
            }

            var finalFileName = fileName ?? $"image_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}";
            
            // Create a memory stream from bytes
            using var ms = new System.IO.MemoryStream(fileBytes);
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(finalFileName, ms),
                Folder = folder ?? _defaultFolder,
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            
            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var secureUrl = uploadResult.SecureUrl.ToString();
                _logger.LogInformation("Successfully uploaded base64 image to Cloudinary: {Url}", secureUrl);
                return (secureUrl, null);
            }
            else
            {
                var error = $"Upload failed with status: {uploadResult.StatusCode}";
                _logger.LogError(error);
                return (null, error);
            }
        }
        catch (Exception ex)
        {
            var error = $"Cloudinary upload error: {ex.Message}";
            _logger.LogError(ex, error);
            return (null, error);
        }
    }

    public async Task<List<CloudinaryImageInfo>> GetAllImagesAsync(string? folder = null)
    {
        try
        {
            var targetFolder = folder ?? _defaultFolder;
            var listParams = new ListResourcesParams
            {
                Type = "upload",
                MaxResults = 500,
                ResourceType = ResourceType.Image
            };

            var listResult = await _cloudinary.ListResourcesAsync(listParams);

            // The commented-out logging statement is incomplete and missing a closing parenthesis and semicolon.
            // To fix the errors, uncomment the line and ensure it is properly closed.

            //_logger.LogInformation("Retrieved {Count} total resources from Cloudinary. Filtering by folder: {Folder}",
            //    listResult.Resources.Count, targetFolder);

            // Filter by folder prefix after retrieval
            var filteredResources = listResult.Resources
                .Where(resource => 
                {
                    var publicId = resource.PublicId ?? string.Empty;
                    var matches = publicId.StartsWith(targetFolder + "/", StringComparison.OrdinalIgnoreCase) || 
                                  publicId == targetFolder;
                    return matches;
                })
                .ToList();

            _logger.LogInformation("Found {Count} images in folder {Folder}", 
                filteredResources.Count, 
                targetFolder);

            return filteredResources
                .Select(resource => new CloudinaryImageInfo
                {
                    PublicId = resource.PublicId,
                    SecureUrl = resource.SecureUrl?.ToString() ?? string.Empty,
                    FileName = resource.PublicId?.Split('/').LastOrDefault() ?? resource.PublicId ?? "unknown",
                    CreatedAt = DateTime.TryParse(resource.CreatedAt, out var date) ? date : DateTime.UtcNow,
                    Width = resource.Width,
                    Height = resource.Height,
                    Format = resource.Format,
                    Bytes = resource.Bytes
                })
                .OrderByDescending(img => img.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve images from Cloudinary");
            return new List<CloudinaryImageInfo>();
        }
    }

    public async Task<CloudinaryImageInfo?> GetImageAsync(string publicId)
    {
        try
        {
            var getResourceParams = new GetResourceParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            var result = await _cloudinary.GetResourceAsync(getResourceParams);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return new CloudinaryImageInfo
                {
                    PublicId = result.PublicId,
                    SecureUrl = result.SecureUrl.ToString(),
                    FileName = result.PublicId.Split('/').LastOrDefault() ?? result.PublicId,
                    CreatedAt = DateTime.TryParse(result.CreatedAt, out var date) ? date : DateTime.UtcNow,
                    Width = result.Width,
                    Height = result.Height,
                    Format = result.Format,
                    Bytes = result.Bytes
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image from Cloudinary: {PublicId}", publicId);
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        try
        {
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };
            
            var deleteResult = await _cloudinary.DestroyAsync(deleteParams);
            
            return deleteResult.Result == "ok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image from Cloudinary: {PublicId}", publicId);
            return false;
        }
    }

    public async Task<List<BulkUploadResult>> UploadImagesAsync(IFormFileCollection files, string? folder = null)
    {
        var results = new List<BulkUploadResult>();

        foreach (var file in files)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder ?? _defaultFolder,
                    UseFilename = true,
                    UniqueFilename = false,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    results.Add(new BulkUploadResult
                    {
                        FileName = file.FileName,
                        SecureUrl = uploadResult.SecureUrl.ToString(),
                        PublicId = uploadResult.PublicId,
                        Success = true,
                        ErrorMessage = null
                    });
                }
                else
                {
                    results.Add(new BulkUploadResult
                    {
                        FileName = file.FileName,
                        SecureUrl = null,
                        PublicId = null,
                        Success = false,
                        ErrorMessage = $"Upload failed with status: {uploadResult.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new BulkUploadResult
                {
                    FileName = file.FileName,
                    SecureUrl = null,
                    PublicId = null,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        return results;
    }
}

public class CloudinaryImageInfo
{
    public string PublicId { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Format { get; set; }
    public long? Bytes { get; set; }
}

public class BulkUploadResult
{
    public string FileName { get; set; } = string.Empty;
    public string? SecureUrl { get; set; }
    public string? PublicId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
