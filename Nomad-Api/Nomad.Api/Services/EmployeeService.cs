using AutoMapper;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<TenantRole> _roleManager;
    private const string DefaultPassword = "Password@123";

    public EmployeeService(
        NomadSurveysDbContext context,
        IMapper mapper,
        ILogger<EmployeeService> logger,
        UserManager<ApplicationUser> userManager,
        RoleManager<TenantRole> roleManager)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
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

            _logger.LogInformation("Processing {Count} employees for bulk create/update using optimized lookup", request.Employees.Count);

            // 1. Bulk Fetch existing records to avoid N+1 queries
            var requestedEmployeeIds = request.Employees
                .Select(e => e.EmployeeId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            
            var requestedEmails = request.Employees
                .Select(e => e.Email.ToLower())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct()
                .ToList();

            // Get all existing employees in this tenant (including inactive ones)
            var existingEmployees = await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => e.TenantId == tenantId && (requestedEmployeeIds.Contains(e.EmployeeId) || requestedEmails.Contains(e.Email.ToLower())))
                .ToListAsync();

            // Create dictionaries for O(1) lookups
            var existingByEmail = existingEmployees
                .Where(e => !string.IsNullOrWhiteSpace(e.Email))
                .ToDictionary(e => e.Email.ToLower(), e => e);

            var existingByEmployeeId = existingEmployees
                .Where(e => !string.IsNullOrWhiteSpace(e.EmployeeId))
                .ToDictionary(e => e.EmployeeId, e => e);

            // 2. Separate new employees from updates using in-memory logic
            for (int i = 0; i < request.Employees.Count; i++)
            {
                var employeeRequest = request.Employees[i];
                var employeeEmailLower = employeeRequest.Email.ToLower();

                // Check for existing record in memory
                existingByEmail.TryGetValue(employeeEmailLower, out var empByEmail);
                existingByEmployeeId.TryGetValue(employeeRequest.EmployeeId, out var empById);

                var existingEmployee = empByEmail ?? empById;

                // Validation: Only error if duplicate exists AND is ACTIVE and not the one we're processing
                // If we found an existing employee (active or inactive), we move to update/reactivate logic
                if (existingEmployee == null)
                {
                    // This is a new employee
                    var employee = _mapper.Map<Employee>(employeeRequest);
                    employee.Id = Guid.NewGuid();
                    employee.TenantId = tenantId;
                    employee.CreatedAt = DateTime.UtcNow;
                    employee.IsActive = true;

                    employeesToCreate.Add(employee);
                    _logger.LogDebug("Employee {EmployeeId} is new - marked for creation", employeeRequest.EmployeeId);
                }
                else
                {
                    // This is an update or reactivation
                    employeesToUpdate.Add((existingEmployee, employeeRequest));
                    _logger.LogDebug("Employee {EmployeeId}/{Email} exists - marked for update/reactivation",
                        employeeRequest.EmployeeId, employeeRequest.Email);
                }
            }

            // 3. Batch DB Operations
            if (employeesToCreate.Any())
            {
                await _context.Employees.AddRangeAsync(employeesToCreate);
                response.SuccessfullyCreated = employeesToCreate.Count;
                response.CreatedIds = employeesToCreate.Select(e => e.Id).ToList();
            }

            if (employeesToUpdate.Any())
            {
                foreach (var (existingEmployee, updateRequest) in employeesToUpdate)
                {
                    existingEmployee.FirstName = updateRequest.FirstName;
                    existingEmployee.LastName = updateRequest.LastName;
                    existingEmployee.Email = updateRequest.Email;
                    existingEmployee.EmployeeId = updateRequest.EmployeeId;
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
                    existingEmployee.IsActive = true;

                    response.UpdatedCount++;
                }
            }

            // Single database hit for all employee changes
            await _context.SaveChangesAsync();

            // 4. Batch User Synchronization
            var allEmployees = employeesToCreate.Concat(employeesToUpdate.Select(x => x.ExistingEmployee)).ToList();
            await SyncEmployeesToUsersAsync(allEmployees, tenantId);

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

            // Sync to User table
            await SyncEmployeesToUsersAsync(new List<Employee> { employee }, employee.TenantId);

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
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var employee = await _context.Employees
                .Include(e => e.Users)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                return false;
            }

            // Soft delete
            employee.IsActive = false;
            employee.UpdatedAt = DateTime.UtcNow;

            // Also deactivate associated users
            foreach (var user in employee.Users)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
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
            .IgnoreQueryFilters() // Must ignore filters to check for soft-deleted duplicates
            .Where(e => e.Email.ToLower() == email.ToLower() && e.TenantId == tenantId);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private async Task<List<string>> ValidateEmployeeAsync(CreateEmployeeRequest request, Guid tenantId, Guid? excludeId = null)
    {
        var errors = new List<string>();

        // We skip this specific check in ValidateEmployeeAsync because BulkCreateEmployeesAsync 
        // will handle it properly by either updating/reactivating or creating.
        // However, for single creation through other means (if any), we still want to ensure 
        // we don't return "already exists" if we are calling from BulkCreate which handles it.
        // Actually, ValidateEmployeeAsync is called from BulkCreate. 
        // If it's a "New" employee request but another "Active" one exists, that's an error.
        // If an "Inactive" one exists, BulkCreate will handle it by moving it to the update list.
        
        // Let's refine this: If an ACTIVE employee exists with same email/ID, it's a conflict IF we're not updating that specific record.
        
        // Check for duplicate ACTIVE email within tenant
        var activeEmailExists = await _context.Employees
            .Where(e => e.Email.ToLower() == request.Email.ToLower() && e.TenantId == tenantId && e.IsActive)
            .Where(e => !excludeId.HasValue || e.Id != excludeId.Value)
            .AnyAsync();

        if (activeEmailExists)
        {
            errors.Add($"An active employee with email {request.Email} already exists in this tenant");
        }

        // Check for duplicate ACTIVE EmployeeId within tenant
        var activeEmployeeIdExists = await _context.Employees
            .Where(e => e.EmployeeId == request.EmployeeId && e.TenantId == tenantId && e.IsActive)
            .Where(e => !excludeId.HasValue || e.Id != excludeId.Value)
            .AnyAsync();

        if (activeEmployeeIdExists)
        {
            errors.Add($"An active employee with EmployeeId {request.EmployeeId} already exists in this tenant");
        }

        return errors;
    }

    /// <summary>
    /// Syncs employee data to User table - creates new users or updates existing ones
    /// </summary>
    private async Task SyncEmployeesToUsersAsync(List<Employee> employees, Guid tenantId)
    {
        try
        {
            if (!employees.Any())
                return;

            var employeeEmails = employees.Select(e => e.Email.ToLower()).ToList();

            // Find users with matching emails in the same tenant (including inactive ones)
            var matchingUsers = await _context.Users
                .IgnoreQueryFilters()
                .Where(u => employeeEmails.Contains(u.Email!.ToLower()) && u.TenantId == tenantId)
                .ToListAsync();

            // Get Participant role
            var participantRole = await _roleManager.FindByNameAsync("Participant");
            if (participantRole == null)
            {
                _logger.LogError("Participant role not found - cannot sync employees to users");
                return;
            }

            var createdCount = 0;
            var updatedCount = 0;

            foreach (var employee in employees)
            {
                var matchingUser = matchingUsers.FirstOrDefault(u =>
                    string.Equals(u.Email, employee.Email, StringComparison.OrdinalIgnoreCase));

                if (matchingUser != null)
                {
                    // Update and reactivate existing user
                    matchingUser.FirstName = employee.FirstName;
                    matchingUser.LastName = employee.LastName;
                    matchingUser.Gender = employee.Gender;
                    matchingUser.Designation = employee.Designation;
                    matchingUser.Department = employee.Department;
                    matchingUser.Tenure = employee.Tenure;
                    matchingUser.Grade = employee.Grade;
                    matchingUser.EmployeeId = employee.Id; // FK to Employee
                    matchingUser.IsActive = true; // Reactivate user
                    matchingUser.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Updated/Reactivated user {UserId} from employee {EmployeeId}",
                        matchingUser.Id, employee.EmployeeId);
                    updatedCount++;
                }
                else
                {
                    // Create new user for this employee
                    var newUser = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = employee.Email,
                        Email = employee.Email,
                        FirstName = employee.FirstName,
                        LastName = employee.LastName,
                        Gender = employee.Gender,
                        Designation = employee.Designation,
                        Department = employee.Department,
                        Tenure = employee.Tenure,
                        Grade = employee.Grade,
                        EmployeeId = employee.Id, // FK to Employee
                        EmailConfirmed = true,
                        IsActive = true,
                        TenantId = tenantId,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(newUser, DefaultPassword);
                    if (result.Succeeded)
                    {
                        // Assign Participant role
                        var userTenantRole = new UserTenantRole
                        {
                            Id = Guid.NewGuid(),
                            UserId = newUser.Id,
                            RoleId = participantRole.Id,
                            TenantId = tenantId,
                            IsActive = true
                        };

                        _context.UserTenantRoles.Add(userTenantRole);

                        _logger.LogInformation("Created user {UserId} for employee {EmployeeId} with default password",
                            newUser.Id, employee.EmployeeId);
                        createdCount++;
                    }
                    else
                    {
                        _logger.LogError("Failed to create user for employee {EmployeeId}: {Errors}",
                            employee.EmployeeId, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Synced employees to users: {Created} created, {Updated} updated/reactivated",
                createdCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing employees to users");
            // Don't throw - this is a non-critical operation
        }
    }
}

