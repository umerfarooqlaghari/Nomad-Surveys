using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeListResponse>> GetEmployeesAsync(
        Guid? tenantId = null,
        string? name = null,
        string? designation = null,
        string? department = null,
        string? email = null);
    
    Task<EmployeeResponse?> GetEmployeeByIdAsync(Guid employeeId);
    Task<BulkCreateResponse> BulkCreateEmployeesAsync(BulkCreateEmployeesRequest request, Guid tenantId);
    Task<EmployeeResponse?> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequest request);
    Task<bool> DeleteEmployeeAsync(Guid employeeId);
    Task<bool> EmployeeExistsAsync(Guid employeeId, Guid? tenantId = null);
    Task<bool> EmployeeExistsByEmailAsync(string email, Guid tenantId, Guid? excludeId = null);
}

