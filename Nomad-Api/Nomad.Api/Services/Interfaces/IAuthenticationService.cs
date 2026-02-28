using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, Guid? tenantId = null);
    Task<bool> AssignRoleAsync(AssignRoleRequest request, Guid? tenantId = null);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword);
    Task<UserResponse?> GetUserByIdAsync(Guid userId);
    Task<List<UserListResponse>> GetUsersAsync(Guid? tenantId = null);
    Task<bool> DeactivateUserAsync(Guid userId);
    Task<bool> ActivateUserAsync(Guid userId);
    Task<List<RoleResponse>> GetRolesAsync(Guid? tenantId = null);
    Task<string> GenerateJwtTokenAsync(Guid userId, Guid? tenantId = null);
    Task<bool> ValidateTokenAsync(string token);
}
