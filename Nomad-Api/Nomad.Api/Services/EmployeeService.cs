using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class EmployeeService : IEmployeeService
{
    private readonly NomadSurveysDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        NomadSurveysDbContext context,
        IMapper mapper,
        ILogger<EmployeeService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<EmployeeListResponse>> GetEmployeesAsync(
        Guid? tenantId = null,
        string? name = null,
        string? designation = null,
        string? department = null,
        string? email = null)
    {
        try
        {
            var query = _context.Employees.Where(e => e.IsActive).AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(e => e.TenantId == tenantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var lowerName = name.ToLower();
                query = query.Where(e => 
                    e.FirstName.ToLower().Contains(lowerName) || 
                    e.LastName.ToLower().Contains(lowerName) ||
                    (e.FirstName + " " + e.LastName).ToLower().Contains(lowerName));
            }

            if (!string.IsNullOrWhiteSpace(designation))
            {
                query = query.Where(e => e.Designation != null && e.Designation.ToLower().Contains(designation.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(e => e.Department != null && e.Department.ToLower().Contains(department.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(e => e.Email.ToLower().Contains(email.ToLower()));
            }

            var employees = await query
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            return _mapper.Map<List<EmployeeListResponse>>(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employees with filters");
            throw;
        }
    }

    public async Task<EmployeeResponse?> GetEmployeeByIdAsync(Guid employeeId)
    {
        try
        {
            var employee = await _context.Employees
                .Include(e => e.Tenant)
                .Include(e => e.Subject)
                .Include(e => e.Evaluator)
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.IsActive);

            if (employee == null)
            {
                return null;
            }

            return _mapper.Map<EmployeeResponse>(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<BulkCreateResponse> BulkCreateEmployeesAsync(BulkCreateEmployeesRequest request, Guid tenantId)
    {
        var response = new BulkCreateResponse
        {
            TotalRequested = request.Employees.Count,
            SuccessfullyCreated = 0,
            UpdatedCount = 0,
            Failed = 0,
            Errors = new List<string>(),
            CreatedIds = new List<Guid>()
        };

        if (request.Employees == null || !request.Employees.Any())
        {
            response.Errors.Add("No employees provided");
            response.Failed = response.TotalRequested;
            return response;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var employeesToCreate = new List<Employee>();
            var employeesToUpdate = new List<(Employee ExistingEmployee, CreateEmployeeRequest UpdateRequest)>();
            var errors = new List<string>();

            _logger.LogInformation("Processing {Count} employees for bulk create/update", request.Employees.Count);

            // Get all existing employees by EmployeeId in this tenant
            var requestedEmployeeIds = request.Employees.Select(e => e.EmployeeId).ToList();
            var existingEmployees = await _context.Employees
                .Where(e => requestedEmployeeIds.Contains(e.EmployeeId) && e.TenantId == tenantId)
                .ToListAsync();

            var existingEmployeeIds = existingEmployees.Select(e => e.EmployeeId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Separate new employees from updates
            for (int i = 0; i < request.Employees.Count; i++)
            {
                var employeeRequest = request.Employees[i];
                var validationErrors = await ValidateEmployeeAsync(employeeRequest, tenantId, null);

                if (validationErrors.Any())
                {
                    errors.AddRange(validationErrors.Select(e => $"Employee {i + 1}: {e}"));
                    continue;
                }

                if (existingEmployeeIds.Contains(employeeRequest.EmployeeId))
                {
                    // This is an update
                    var existingEmployee = existingEmployees.First(e => e.EmployeeId == employeeRequest.EmployeeId);
                    employeesToUpdate.Add((existingEmployee, employeeRequest));
                    _logger.LogInformation("Employee {EmployeeId} exists - will update", employeeRequest.EmployeeId);
                }
                else
                {
                    // This is a new employee
                    var employee = _mapper.Map<Employee>(employeeRequest);
                    employee.Id = Guid.NewGuid();
                    employee.TenantId = tenantId;
                    employee.CreatedAt = DateTime.UtcNow;
                    employee.IsActive = true;

                    employeesToCreate.Add(employee);
                    _logger.LogInformation("Employee {EmployeeId} is new - will create", employeeRequest.EmployeeId);
                }
            }

            // Create new employees
            if (employeesToCreate.Any())
            {
                await _context.Employees.AddRangeAsync(employeesToCreate);
                response.SuccessfullyCreated = employeesToCreate.Count;
                response.CreatedIds = employeesToCreate.Select(e => e.Id).ToList();
                _logger.LogInformation("Created {Count} new employees", employeesToCreate.Count);
            }

            // Update existing employees
            if (employeesToUpdate.Any())
            {
                foreach (var (existingEmployee, updateRequest) in employeesToUpdate)
                {
                    _logger.LogInformation("Updating existing employee {EmployeeId}", existingEmployee.EmployeeId);

                    existingEmployee.FirstName = updateRequest.FirstName;
                    existingEmployee.LastName = updateRequest.LastName;
                    existingEmployee.Email = updateRequest.Email;
                    existingEmployee.Number = updateRequest.Number;
                    existingEmployee.CompanyName = updateRequest.CompanyName;
                    existingEmployee.Designation = updateRequest.Designation;
                    existingEmployee.Department = updateRequest.Department;
                    existingEmployee.Tenure = updateRequest.Tenure;
                    existingEmployee.Grade = updateRequest.Grade;
                    existingEmployee.Gender = updateRequest.Gender;
                    existingEmployee.ManagerId = updateRequest.ManagerId;
                    existingEmployee.MoreInfo = updateRequest.MoreInfo;
                    existingEmployee.UpdatedAt = DateTime.UtcNow;

                    response.UpdatedCount++;
                }
            }

            // Single database hit for all changes
            await _context.SaveChangesAsync();

            // Sync with User table
            var allEmployees = employeesToCreate.Concat(employeesToUpdate.Select(x => x.ExistingEmployee)).ToList();
            await SyncEmployeesToUsersAsync(allEmployees, tenantId);

            // Calculate totals including both created and updated
            var totalProcessed = response.SuccessfullyCreated + response.UpdatedCount;
            response.Failed = response.TotalRequested - totalProcessed;
            response.Errors = errors;

            await transaction.CommitAsync();

            _logger.LogInformation("Bulk processed {TotalProcessed} employees for tenant {TenantId}: {Created} created, {Updated} updated",
                totalProcessed, tenantId, response.SuccessfullyCreated, response.UpdatedCount);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error bulk creating employees for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<EmployeeResponse?> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequest request)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return null;
            }

            // Validate the update
            var createRequest = _mapper.Map<CreateEmployeeRequest>(request);
            var validationErrors = await ValidateEmployeeAsync(
                createRequest,
                employee.TenantId,
                employeeId);

            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(", ", validationErrors));
            }

            // Update employee
            _mapper.Map(request, employee);
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetEmployeeByIdAsync(employeeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<bool> DeleteEmployeeAsync(Guid employeeId)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return false;
            }

            // Soft delete
            employee.IsActive = false;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<bool> EmployeeExistsAsync(Guid employeeId, Guid? tenantId = null)
    {
        var query = _context.Employees.Where(e => e.Id == employeeId && e.IsActive);

        if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> EmployeeExistsByEmailAsync(string email, Guid tenantId, Guid? excludeId = null)
    {
        var query = _context.Employees
            .Where(e => e.Email.ToLower() == email.ToLower() && e.TenantId == tenantId && e.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private async Task<List<string>> ValidateEmployeeAsync(CreateEmployeeRequest request, Guid tenantId, Guid? excludeId = null)
    {
        var errors = new List<string>();

        // Check for duplicate email within tenant
        if (await EmployeeExistsByEmailAsync(request.Email, tenantId, excludeId))
        {
            errors.Add($"Employee with email {request.Email} already exists in this tenant");
        }

        // Check for duplicate EmployeeId within tenant
        var employeeIdExists = await _context.Employees
            .Where(e => e.EmployeeId == request.EmployeeId && e.TenantId == tenantId && e.IsActive)
            .Where(e => !excludeId.HasValue || e.Id != excludeId.Value)
            .AnyAsync();

        if (employeeIdExists)
        {
            errors.Add($"Employee with EmployeeId {request.EmployeeId} already exists in this tenant");
        }

        return errors;
    }

    /// <summary>
    /// Syncs employee data to User table for employees that have matching email addresses
    /// </summary>
    private async Task SyncEmployeesToUsersAsync(List<Employee> employees, Guid tenantId)
    {
        try
        {
            if (!employees.Any())
                return;

            var employeeEmails = employees.Select(e => e.Email.ToLower()).ToList();

            // Find users with matching emails in the same tenant
            var matchingUsers = await _context.Users
                .Where(u => employeeEmails.Contains(u.Email!.ToLower()) && u.TenantId == tenantId)
                .ToListAsync();

            foreach (var employee in employees)
            {
                var matchingUser = matchingUsers.FirstOrDefault(u =>
                    string.Equals(u.Email, employee.Email, StringComparison.OrdinalIgnoreCase));

                if (matchingUser != null)
                {
                    // Sync employee data to user
                    matchingUser.FirstName = employee.FirstName;
                    matchingUser.LastName = employee.LastName;
                    matchingUser.Gender = employee.Gender;
                    matchingUser.Designation = employee.Designation;
                    matchingUser.Department = employee.Department;
                    matchingUser.Tenure = employee.Tenure;
                    matchingUser.Grade = employee.Grade;
                    matchingUser.EmployeeId = employee.Id; // FK to Employee
                    matchingUser.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Synced employee {EmployeeId} data to user {UserId}",
                        employee.EmployeeId, matchingUser.Id);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} employees to users", matchingUsers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing employees to users");
            // Don't throw - this is a non-critical operation
        }
    }
}

