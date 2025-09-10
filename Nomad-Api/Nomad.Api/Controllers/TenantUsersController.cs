using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/users")]
[AuthorizeTenant]
public class TenantUsersController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<TenantUsersController> _logger;

    public TenantUsersController(IAuthenticationService authenticationService, ILogger<TenantUsersController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all users in the current tenant (TenantAdmin only)
    /// </summary>
    [HttpGet]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<List<UserListResponse>>> GetUsers()
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context not found" });
            }

            var users = await _authenticationService.GetUsersAsync(tenantId.Value);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for tenant");
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Get user by ID (TenantAdmin only)
    /// </summary>
    [HttpGet("{id:guid}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
        try
        {
            var user = await _authenticationService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Ensure user belongs to current tenant
            var tenantId = GetCurrentTenantId();
            if (user.TenantId != tenantId)
            {
                return NotFound();
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the user" });
        }
    }

    /// <summary>
    /// Create a new user in the current tenant (TenantAdmin only)
    /// </summary>
    [HttpPost]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context not found" });
            }

            var user = await _authenticationService.CreateUserAsync(request, tenantId.Value);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create user: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    /// <summary>
    /// Assign role to user (TenantAdmin only)
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context not found" });
            }

            // Ensure user belongs to current tenant
            var user = await _authenticationService.GetUserByIdAsync(id);
            if (user == null || user.TenantId != tenantId)
            {
                return NotFound();
            }

            request.UserId = id;
            var result = await _authenticationService.AssignRoleAsync(request, tenantId.Value);
            
            if (result)
            {
                return Ok(new { message = "Role assigned successfully" });
            }

            return BadRequest(new { message = "Failed to assign role" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while assigning the role" });
        }
    }

    /// <summary>
    /// Deactivate user (TenantAdmin only)
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult> DeactivateUser(Guid id)
    {
        try
        {
            // Ensure user belongs to current tenant
            var user = await _authenticationService.GetUserByIdAsync(id);
            var tenantId = GetCurrentTenantId();
            
            if (user == null || user.TenantId != tenantId)
            {
                return NotFound();
            }

            var result = await _authenticationService.DeactivateUserAsync(id);
            if (result)
            {
                return Ok(new { message = "User deactivated successfully" });
            }

            return BadRequest(new { message = "Failed to deactivate user" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while deactivating the user" });
        }
    }

    /// <summary>
    /// Activate user (TenantAdmin only)
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult> ActivateUser(Guid id)
    {
        try
        {
            // Ensure user belongs to current tenant
            var user = await _authenticationService.GetUserByIdAsync(id);
            var tenantId = GetCurrentTenantId();
            
            if (user == null || user.TenantId != tenantId)
            {
                return NotFound();
            }

            var result = await _authenticationService.ActivateUserAsync(id);
            if (result)
            {
                return Ok(new { message = "User activated successfully" });
            }

            return BadRequest(new { message = "Failed to activate user" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while activating the user" });
        }
    }

    /// <summary>
    /// Get available roles for the current tenant (TenantAdmin only)
    /// </summary>
    [HttpGet("roles")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<List<RoleResponse>>> GetRoles()
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context not found" });
            }

            var roles = await _authenticationService.GetRolesAsync(tenantId.Value);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for tenant");
            return StatusCode(500, new { message = "An error occurred while retrieving roles" });
        }
    }
}
