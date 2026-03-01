using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Scripts;

/// <summary>
/// Migration script to clean Users table and migrate all employees to Users
/// Run this from Program.cs or create a separate console app
/// </summary>
public class MigrateEmployeesToUsers
{
    private readonly NomadSurveysDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<TenantRole> _roleManager;
    private readonly ILogger<MigrateEmployeesToUsers> _logger;
    private readonly IPasswordGenerator _passwordGenerator;

    public MigrateEmployeesToUsers(
        NomadSurveysDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<TenantRole> roleManager,
        ILogger<MigrateEmployeesToUsers> logger,
        IPasswordGenerator passwordGenerator)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _passwordGenerator = passwordGenerator;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting employee to user migration...");

        await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Step 1: Delete all users except SuperAdmin
                await CleanUsersTableAsync();

                // Step 2: Migrate all employees to Users table
                await MigrateEmployeesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("✅ Migration completed successfully!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Migration failed!");
                throw;
            }
        });
    }

    private async Task CleanUsersTableAsync()
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

        _logger.LogInformation("Deleted {Count} UserTenantRoles", userTenantRolesToDelete.Count);

        // Delete all users except SuperAdmin
        var usersToDelete = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id != superAdmin.Id)
            .ToListAsync();

        _context.Users.RemoveRange(usersToDelete);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} users (kept SuperAdmin)", usersToDelete.Count);
    }

    private async Task MigrateEmployeesAsync()
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

                var password = _passwordGenerator.Generate(employee.Email);
                var result = await _userManager.CreateAsync(newUser, password);
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
    }
}

