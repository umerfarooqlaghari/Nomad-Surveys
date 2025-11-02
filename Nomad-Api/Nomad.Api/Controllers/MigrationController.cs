using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Authorization;
using Nomad.Api.Data;
using Nomad.Api.Entities;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly NomadSurveysDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<TenantRole> _roleManager;
    private readonly ILogger<MigrationController> _logger;
    private const string DefaultPassword = "Password@123";

    public MigrationController(
        NomadSurveysDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<TenantRole> roleManager,
        ILogger<MigrationController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Clean Users table and migrate all employees to Users (SuperAdmin only)
    /// </summary>
    [HttpPost("migrate-employees-to-users")]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult> MigrateEmployeesToUsers()
    {
        _logger.LogInformation("Starting employee to user migration...");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Step 1: Delete all users except SuperAdmin
            var cleanResult = await CleanUsersTableAsync();

            // Step 2: Migrate all employees to Users table
            var migrateResult = await MigrateEmployeesAsync();

            await transaction.CommitAsync();

            var result = new
            {
                success = true,
                message = "Migration completed successfully!",
                usersDeleted = cleanResult.UsersDeleted,
                rolesDeleted = cleanResult.RolesDeleted,
                employeesMigrated = migrateResult.Created,
                employeesFailed = migrateResult.Failed,
                totalEmployees = migrateResult.Total,
                defaultPassword = DefaultPassword
            };

            _logger.LogInformation("✅ Migration completed: {Result}", result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "❌ Migration failed!");
            return StatusCode(500, new { success = false, message = "Migration failed", error = ex.Message });
        }
    }

    private async Task<(int UsersDeleted, int RolesDeleted)> CleanUsersTableAsync()
    {
        _logger.LogInformation("Step 1: Cleaning Users table (keeping only SuperAdmin)...");

        // Get SuperAdmin user
        var superAdmin = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "superadmin@nomadsurveys.com");

        if (superAdmin == null)
        {
            throw new InvalidOperationException("SuperAdmin user not found!");
        }

        // Delete all UserTenantRoles except SuperAdmin's
        var userTenantRolesToDelete = await _context.UserTenantRoles
            .Where(utr => utr.UserId != superAdmin.Id)
            .ToListAsync();

        _context.UserTenantRoles.RemoveRange(userTenantRolesToDelete);
        await _context.SaveChangesAsync();

        var rolesDeleted = userTenantRolesToDelete.Count;
        _logger.LogInformation("Deleted {Count} UserTenantRoles", rolesDeleted);

        // Delete all users except SuperAdmin
        var usersToDelete = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id != superAdmin.Id)
            .ToListAsync();

        _context.Users.RemoveRange(usersToDelete);
        await _context.SaveChangesAsync();

        var usersDeleted = usersToDelete.Count;
        _logger.LogInformation("Deleted {Count} users (kept SuperAdmin)", usersDeleted);

        return (usersDeleted, rolesDeleted);
    }

    private async Task<(int Created, int Failed, int Total)> MigrateEmployeesAsync()
    {
        _logger.LogInformation("Step 2: Migrating employees to Users table...");

        // Get Participant role
        var participantRole = await _roleManager.FindByNameAsync("Participant");
        if (participantRole == null)
        {
            throw new InvalidOperationException("Participant role not found!");
        }

        // Get all active employees
        var employees = await _context.Employees
            .Where(e => e.IsActive)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        _logger.LogInformation("Found {Count} active employees to migrate", employees.Count);

        var createdCount = 0;
        var failedCount = 0;

        foreach (var employee in employees)
        {
            try
            {
                // Check if user already exists with this email
                var existingUser = await _userManager.FindByEmailAsync(employee.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("User already exists for employee {EmployeeId} ({Email}), skipping...",
                        employee.EmployeeId, employee.Email);
                    continue;
                }

                // Create new user
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
                    TenantId = employee.TenantId,
                    CreatedAt = employee.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
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
                        TenantId = employee.TenantId,
                        IsActive = true
                    };

                    _context.UserTenantRoles.Add(userTenantRole);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("✅ Created user for employee: {Name} ({Email}) - User ID: {UserId}",
                        $"{employee.FirstName} {employee.LastName}",
                        employee.Email,
                        newUser.Id);

                    createdCount++;
                }
                else
                {
                    _logger.LogError("❌ Failed to create user for employee {EmployeeId} ({Email}): {Errors}",
                        employee.EmployeeId,
                        employee.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating user for employee {EmployeeId} ({Email})",
                    employee.EmployeeId, employee.Email);
                failedCount++;
            }
        }

        _logger.LogInformation("Migration summary: {Created} created, {Failed} failed out of {Total} employees",
            createdCount, failedCount, employees.Count);

        return (createdCount, failedCount, employees.Count);
    }

    /// <summary>
    /// Create missing Evaluator and Subject records for all employees (SuperAdmin only)
    /// </summary>
    [HttpPost("create-missing-evaluators-subjects")]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult> CreateMissingEvaluatorsAndSubjects()
    {
        _logger.LogInformation("Starting creation of missing Evaluator and Subject records...");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await CreateMissingRecordsAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("✅ Creation completed: {Result}", result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "❌ Creation failed!");
            return StatusCode(500, new { success = false, message = "Creation failed", error = ex.Message });
        }
    }

    private async Task<object> CreateMissingRecordsAsync()
    {
        // Get all active employees
        var employees = await _context.Employees
            .Include(e => e.Subject)
            .Include(e => e.Evaluator)
            .Where(e => e.IsActive)
            .ToListAsync();

        _logger.LogInformation("Found {Count} active employees", employees.Count);

        var evaluatorsCreated = 0;
        var subjectsCreated = 0;
        var errors = new List<string>();

        foreach (var employee in employees)
        {
            try
            {
                // Get the user for this employee (if exists)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);

                // Create Evaluator if missing
                if (employee.Evaluator == null)
                {
                    var evaluator = new Evaluator
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        TenantId = employee.TenantId,
                        UserId = user?.Id
                    };

                    _context.Evaluators.Add(evaluator);
                    evaluatorsCreated++;

                    _logger.LogInformation("✅ Created Evaluator for employee: {Name} ({Email})",
                        $"{employee.FirstName} {employee.LastName}",
                        employee.Email);
                }

                // Create Subject if missing
                if (employee.Subject == null)
                {
                    var subject = new Subject
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        TenantId = employee.TenantId,
                        UserId = user?.Id
                    };

                    _context.Subjects.Add(subject);
                    subjectsCreated++;

                    _logger.LogInformation("✅ Created Subject for employee: {Name} ({Email})",
                        $"{employee.FirstName} {employee.LastName}",
                        employee.Email);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error creating records for employee {employee.EmployeeId} ({employee.Email}): {ex.Message}";
                _logger.LogError(ex, errorMsg);
                errors.Add(errorMsg);
            }
        }

        await _context.SaveChangesAsync();

        // Verify the results
        var verification = await _context.Employees
            .Include(e => e.Subject)
            .Include(e => e.Evaluator)
            .Include(e => e.Users)
            .Where(e => e.IsActive)
            .Select(e => new
            {
                EmployeeId = e.EmployeeId,
                Name = $"{e.FirstName} {e.LastName}",
                Email = e.Email,
                HasSubject = e.Subject != null,
                HasEvaluator = e.Evaluator != null,
                HasUser = e.Users.Any()
            })
            .ToListAsync();

        return new
        {
            success = true,
            message = "Creation completed successfully!",
            evaluatorsCreated,
            subjectsCreated,
            totalEmployees = employees.Count,
            errors,
            verification
        };
    }
}

