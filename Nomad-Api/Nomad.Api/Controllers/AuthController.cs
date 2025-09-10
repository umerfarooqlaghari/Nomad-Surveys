using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;
using System.Security.Claims;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// SuperAdmin login (no tenant required)
    /// </summary>
    [HttpPost("superadmin/login")]
    public async Task<ActionResult<LoginResponse>> SuperAdminLogin([FromBody] SuperAdminLoginRequest request)
    {
        try
        {
            var loginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password,
                TenantSlug = "", // Empty for SuperAdmin
                RememberMe = request.RememberMe
            };

            var response = await _authenticationService.LoginAsync(loginRequest);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("SuperAdmin login failed for {Email}: {Message}",
                request.Email, ex.Message);
            return Unauthorized(new { message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SuperAdmin login for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Login to a specific tenant
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authenticationService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed for {Email} on tenant {TenantSlug}: {Message}", 
                request.Email, request.TenantSlug, ex.Message);
            return Unauthorized(new { message = "Invalid credentials or tenant access" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email} on tenant {TenantSlug}", request.Email, request.TenantSlug);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _authenticationService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    /// <summary>
    /// Change password for current user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _authenticationService.ChangePasswordAsync(userId, request);
            if (result)
            {
                return Ok(new { message = "Password changed successfully" });
            }

            return BadRequest(new { message = "Failed to change password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user");
            return StatusCode(500, new { message = "An error occurred while changing password" });
        }
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    [HttpPost("validate-token")]
    public async Task<ActionResult> ValidateToken([FromBody] string token)
    {
        try
        {
            var isValid = await _authenticationService.ValidateTokenAsync(token);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, new { message = "An error occurred while validating token" });
        }
    }
}
