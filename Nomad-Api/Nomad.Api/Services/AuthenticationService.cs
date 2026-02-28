using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nomad.Api.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<TenantRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly NomadSurveysDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<TenantRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        NomadSurveysDbContext context,
        IMapper mapper,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            Tenant? tenant = null;

            // Find tenant if provided (not required for SuperAdmin)
            if (!string.IsNullOrEmpty(request.TenantSlug) && request.TenantSlug.Trim() != "")
            {
                tenant = await _context.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == request.TenantSlug && t.IsActive);

                if (tenant == null)
                {
                    throw new UnauthorizedAccessException("Invalid tenant");
                }
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Check if user belongs to tenant (or is SuperAdmin)
            var userRoles = await GetUserRolesAsync(user.Id, tenant?.Id);
            var isSuperAdmin = userRoles.Any(r => r == "SuperAdmin");

            // SuperAdmin can login without tenant, others need valid tenant
            if (!isSuperAdmin)
            {
                if (tenant == null)
                {
                    throw new UnauthorizedAccessException("Tenant is required for non-SuperAdmin users");
                }

                if (user.TenantId != tenant.Id)
                {
                    throw new UnauthorizedAccessException("User does not belong to this tenant");
                }
            }

            // Verify password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user.Id, isSuperAdmin ? null : tenant?.Id);

            // Map response
            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Roles = userRoles;
            userResponse.Permissions = await GetUserPermissionsAsync(user.Id, tenant?.Id);

            // Include tenant in user response for easier access on frontend
            var tenantResponse = _mapper.Map<TenantResponse>(tenant);
            userResponse.Tenant = tenantResponse;

            return new LoginResponse
            {
                AccessToken = token,
                RefreshToken = Guid.NewGuid().ToString(), // TODO: Implement proper refresh token
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = userResponse,
                Tenant = tenantResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email {Email} on tenant {TenantSlug}", request.Email, request.TenantSlug);
            throw;
        }
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, Guid? tenantId = null)
    {
        try
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = tenantId,
                EmailConfirmed = true // Auto-confirm for now
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Assign roles
            foreach (var roleName in request.Roles)
            {
                await AssignRoleAsync(new AssignRoleRequest { UserId = user.Id, RoleName = roleName }, tenantId);
            }

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Roles = request.Roles;

            _logger.LogInformation("User created successfully: {UserId} for tenant {TenantId}", user.Id, tenantId);
            return userResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user with email {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> AssignRoleAsync(AssignRoleRequest request, Guid? tenantId = null)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return false;
            }

            var role = await _roleManager.FindByNameAsync(request.RoleName);
            if (role == null)
            {
                return false;
            }

            // Create UserTenantRole entry
            var userTenantRole = new UserTenantRole
            {
                UserId = request.UserId,
                RoleId = role.Id,
                TenantId = tenantId,
                ExpiresAt = request.ExpiresAt,
                IsActive = true
            };

            _context.UserTenantRoles.Add(userTenantRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {RoleName} assigned to user {UserId} for tenant {TenantId}", 
                request.RoleName, request.UserId, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign role {RoleName} to user {UserId}", request.RoleName, request.UserId);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return true;
            }

            _logger.LogWarning("Password change failed for user {UserId}: {Errors}", 
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            // Generate a reset token (atomic and safe way to update password without knowing old one)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successfully for user {UserId}", userId);
                return true;
            }

            _logger.LogWarning("Password reset failed for user {UserId}: {Errors}", 
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<UserResponse?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Roles = await GetUserRolesAsync(userId, user.TenantId);
            userResponse.Permissions = await GetUserPermissionsAsync(userId, user.TenantId);

            return userResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<List<UserListResponse>> GetUsersAsync(Guid? tenantId = null)
    {
        try
        {
            var query = _context.Users.AsQueryable();
            
            if (tenantId.HasValue)
            {
                query = query.Where(u => u.TenantId == tenantId);
            }

            var users = await query.ToListAsync();
            var userResponses = new List<UserListResponse>();

            foreach (var user in users)
            {
                var userResponse = _mapper.Map<UserListResponse>(user);
                userResponse.Roles = await GetUserRolesAsync(user.Id, user.TenantId);
                userResponses.Add(userResponse);
            }

            return userResponses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for tenant {TenantId}", tenantId);
            return new List<UserListResponse>();
        }
    }

    public async Task<bool> DeactivateUserAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} deactivated successfully", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ActivateUserAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} activated successfully", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            return false;
        }
    }

    public async Task<List<RoleResponse>> GetRolesAsync(Guid? tenantId = null)
    {
        try
        {
            var query = _context.Roles.AsQueryable();
            
            if (tenantId.HasValue)
            {
                query = query.Where(r => r.TenantId == tenantId || r.TenantId == null);
            }

            var roles = await query
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            return _mapper.Map<List<RoleResponse>>(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for tenant {TenantId}", tenantId);
            return new List<RoleResponse>();
        }
    }

    public async Task<string> GenerateJwtTokenAsync(Guid userId, Guid? tenantId = null)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        var roles = await GetUserRolesAsync(userId, tenantId);
        var permissions = await GetUserPermissionsAsync(userId, tenantId);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("FirstName", user.FirstName),
            new("LastName", user.LastName)
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim("TenantId", tenantId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<string>> GetUserRolesAsync(Guid userId, Guid? tenantId)
    {
        var userRoles = await _context.UserTenantRoles
            .IgnoreQueryFilters()
            .Include(utr => utr.Role)
            .Where(utr => utr.UserId == userId && 
                         utr.IsActive && 
                         (utr.ExpiresAt == null || utr.ExpiresAt > DateTime.UtcNow) &&
                         (tenantId == null || utr.TenantId == tenantId || utr.TenantId == null))
            .Select(utr => utr.Role.Name!)
            .ToListAsync();

        return userRoles;
    }

    private async Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid? tenantId)
    {
        var permissions = await _context.UserTenantRoles
            .IgnoreQueryFilters()
            .Include(utr => utr.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Where(utr => utr.UserId == userId && 
                         utr.IsActive && 
                         (utr.ExpiresAt == null || utr.ExpiresAt > DateTime.UtcNow) &&
                         (tenantId == null || utr.TenantId == tenantId || utr.TenantId == null))
            .SelectMany(utr => utr.Role.RolePermissions)
            .Where(rp => rp.IsActive)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        return permissions;
    }
}
