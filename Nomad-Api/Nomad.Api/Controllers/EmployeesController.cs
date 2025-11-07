using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId()
    {
        return HttpContext.Items["TenantId"] as Guid?;
    }

    /// <summary>
    /// Get all employees with optional filtering
    /// </summary>
    /// <param name="name">Filter by name (first name, last name, or full name)</param>
    /// <param name="designation">Filter by designation</param>
    /// <param name="department">Filter by department</param>
    /// <param name="email">Filter by email</param>
    /// <returns>List of employees</returns>
    [HttpGet]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<List<EmployeeListResponse>>> GetEmployees(
        [FromQuery] string? name = null,
        [FromQuery] string? designation = null,
        [FromQuery] string? department = null,
        [FromQuery] string? email = null)
    {
        try
        {
            var currentTenantId = GetCurrentTenantId();
            var employees = await _employeeService.GetEmployeesAsync(
                currentTenantId,
                name,
                designation,
                department,
                email);

            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employees");
            return StatusCode(500, new { message = "An error occurred while retrieving employees" });
        }
    }

    /// <summary>
    /// Get a specific employee by ID
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Employee details</returns>
    [HttpGet("{id}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<EmployeeResponse>> GetEmployee(Guid id)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            
            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && employee.TenantId != currentTenantId)
            {
                return Forbid("You can only access employees from your own tenant");
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee {EmployeeId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the employee" });
        }
    }

    /// <summary>
    /// Create a single employee (uses bulk endpoint)
    /// </summary>
    /// <param name="request">Employee creation request</param>
    /// <returns>Created employee</returns>
    [HttpPost]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<BulkCreateResponse>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentTenantId = GetCurrentTenantId();
            if (!currentTenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context is required" });
            }

            var bulkRequest = new BulkCreateEmployeesRequest
            {
                Employees = new List<CreateEmployeeRequest> { request }
            };

            var result = await _employeeService.BulkCreateEmployeesAsync(bulkRequest, currentTenantId.Value);

            if (result.SuccessfullyCreated == 0)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return StatusCode(500, new
            {
                message = ex.Message,
                stackTrace = ex.StackTrace,
                inner = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Bulk create employees (works for single employee as well)
    /// </summary>
    /// <param name="request">Bulk create request with list of employees</param>
    /// <returns>Bulk creation result</returns>
    [HttpPost("bulk")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<BulkCreateResponse>> BulkCreateEmployees([FromBody] BulkCreateEmployeesRequest request)
    {
        try
        {
            _logger.LogInformation("Received bulk create employees request with {Count} employees", request?.Employees?.Count ?? 0);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            var currentTenantId = GetCurrentTenantId();
            if (!currentTenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant context is required" });
            }

            var result = await _employeeService.BulkCreateEmployeesAsync(request, currentTenantId.Value);

            var totalProcessed = result.SuccessfullyCreated + result.UpdatedCount;
            _logger.LogInformation("Bulk processed {TotalProcessed}/{TotalRequested} employees for tenant {TenantId}: {Created} created, {Updated} updated, {Failed} failed",
                totalProcessed, result.TotalRequested, currentTenantId, result.SuccessfullyCreated, result.UpdatedCount, result.Failed);

            if (totalProcessed == 0)
            {
                return BadRequest(result);
            }

            if (result.Failed > 0)
            {
                return StatusCode(207, result); // Multi-Status for partial success
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating employees");
            return StatusCode(500, new { message = "An error occurred while bulk creating employees" });
        }
    }

    /// <summary>
    /// Update an employee
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated employee</returns>
    [HttpPut("{id}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if employee exists and user has access
            var existingEmployee = await _employeeService.GetEmployeeByIdAsync(id);
            if (existingEmployee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && existingEmployee.TenantId != currentTenantId)
            {
                return Forbid("You can only update employees from your own tenant");
            }

            var updatedEmployee = await _employeeService.UpdateEmployeeAsync(id, request);
            
            if (updatedEmployee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            _logger.LogInformation("Updated employee {EmployeeId}", id);
            
            return Ok(updatedEmployee);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating employee {EmployeeId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the employee" });
        }
    }

    /// <summary>
    /// Delete an employee (soft delete)
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [AuthorizeTenantAdmin]
    public async Task<ActionResult> DeleteEmployee(Guid id)
    {
        try
        {
            // Check if employee exists and user has access
            var existingEmployee = await _employeeService.GetEmployeeByIdAsync(id);
            if (existingEmployee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            var currentTenantId = GetCurrentTenantId();
            if (currentTenantId.HasValue && existingEmployee.TenantId != currentTenantId)
            {
                return Forbid("You can only delete employees from your own tenant");
            }

            var result = await _employeeService.DeleteEmployeeAsync(id);
            
            if (!result)
            {
                return NotFound(new { message = "Employee not found" });
            }

            _logger.LogInformation("Deleted employee {EmployeeId}", id);
            
            return Ok(new { message = "Employee deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the employee" });
        }
    }
}

