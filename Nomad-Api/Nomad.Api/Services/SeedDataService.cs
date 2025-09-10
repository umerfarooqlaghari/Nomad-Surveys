using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.Entities;

namespace Nomad.Api.Services;

public class SeedDataService
{
    private readonly NomadSurveysDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<TenantRole> _roleManager;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        NomadSurveysDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<TenantRole> roleManager,
        ILogger<SeedDataService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
            
            await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedSuperAdminAsync();
            await SeedSampleTenantAsync();
            
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private async Task SeedPermissionsAsync()
    {
        var permissions = new[]
        {
            new Permission { Id = Guid.NewGuid(), Name = "manage_users", DisplayName = "Manage Users", Description = "Create, update, and delete users", Category = "User Management" },
            new Permission { Id = Guid.NewGuid(), Name = "manage_surveys", DisplayName = "Manage Surveys", Description = "Create, update, and delete surveys", Category = "Survey Management" },
            new Permission { Id = Guid.NewGuid(), Name = "view_reports", DisplayName = "View Reports", Description = "View survey reports and analytics", Category = "Reporting" },
            new Permission { Id = Guid.NewGuid(), Name = "fill_surveys", DisplayName = "Fill Surveys", Description = "Complete assigned surveys", Category = "Survey Participation" },
            new Permission { Id = Guid.NewGuid(), Name = "assign_surveys", DisplayName = "Assign Surveys", Description = "Assign surveys to participants", Category = "Survey Management" },
            new Permission { Id = Guid.NewGuid(), Name = "manage_company", DisplayName = "Manage Company", Description = "Update company information", Category = "Company Management" }
        };

        foreach (var permission in permissions)
        {
            var existingPermission = await _context.Permissions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Name == permission.Name);

            if (existingPermission == null)
            {
                _context.Permissions.Add(permission);
                _logger.LogInformation("Added permission: {PermissionName}", permission.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new { Name = "SuperAdmin", Description = "Global administrator with full access to all tenants", TenantId = (Guid?)null },
            new { Name = "TenantAdmin", Description = "Tenant administrator with full access within their tenant", TenantId = (Guid?)null },
            new { Name = "Participant", Description = "Survey participant who can fill assigned surveys", TenantId = (Guid?)null }
        };

        foreach (var roleInfo in roles)
        {
            var existingRole = await _roleManager.FindByNameAsync(roleInfo.Name);
            if (existingRole == null)
            {
                var role = new TenantRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleInfo.Name,
                    NormalizedName = roleInfo.Name.ToUpperInvariant(),
                    Description = roleInfo.Description,
                    TenantId = roleInfo.TenantId,
                    IsActive = true
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    await AssignPermissionsToRoleAsync(role);
                    _logger.LogInformation("Created role: {RoleName}", roleInfo.Name);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}", 
                        roleInfo.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task AssignPermissionsToRoleAsync(TenantRole role)
    {
        var permissions = await _context.Permissions.IgnoreQueryFilters().ToListAsync();
        var rolePermissions = new List<RolePermission>();

        switch (role.Name)
        {
            case "SuperAdmin":
                // SuperAdmin gets all permissions
                rolePermissions.AddRange(permissions.Select(p => new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    IsActive = true
                }));
                break;

            case "TenantAdmin":
                // TenantAdmin gets most permissions except global management
                var tenantAdminPermissions = permissions.Where(p => p.Name != "manage_tenants").ToList();
                rolePermissions.AddRange(tenantAdminPermissions.Select(p => new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    IsActive = true
                }));
                break;

            case "Participant":
                // Participant only gets survey filling permission
                var participantPermissions = permissions.Where(p => p.Name == "fill_surveys").ToList();
                rolePermissions.AddRange(participantPermissions.Select(p => new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    IsActive = true
                }));
                break;
        }

        if (rolePermissions.Any())
        {
            _context.RolePermissions.AddRange(rolePermissions);
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedSuperAdminAsync()
    {
        const string superAdminEmail = "superadmin@nomadsurveys.com";
        const string superAdminPassword = "SuperAdmin123!";

        var existingSuperAdmin = await _userManager.FindByEmailAsync(superAdminEmail);
        if (existingSuperAdmin == null)
        {
            var superAdmin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true,
                TenantId = null // SuperAdmin doesn't belong to any specific tenant
            };

            var result = await _userManager.CreateAsync(superAdmin, superAdminPassword);
            if (result.Succeeded)
            {
                // Assign SuperAdmin role
                var superAdminRole = await _roleManager.FindByNameAsync("SuperAdmin");
                if (superAdminRole != null)
                {
                    var userTenantRole = new UserTenantRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = superAdmin.Id,
                        RoleId = superAdminRole.Id,
                        TenantId = null, // Global role
                        IsActive = true
                    };

                    _context.UserTenantRoles.Add(userTenantRole);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created SuperAdmin user: {Email}", superAdminEmail);
            }
            else
            {
                _logger.LogError("Failed to create SuperAdmin: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedSampleTenantAsync()
    {
        const string tenantSlug = "acme-corp";
        
        var existingTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug);

        if (existingTenant == null)
        {
            // Create sample tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Acme Corporation",
                Slug = tenantSlug,
                Description = "Sample tenant for demonstration purposes",
                IsActive = true
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Create sample company
            var company = new Company
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Acme Corporation",
                NumberOfEmployees = 500,
                Location = "New York, USA",
                Industry = "Technology",
                ContactPersonName = "John Doe",
                ContactPersonEmail = "john.doe@acmecorp.com",
                ContactPersonRole = "HR Manager"
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Create tenant admin
            var tenantAdmin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin@acmecorp.com",
                Email = "admin@acmecorp.com",
                FirstName = "John",
                LastName = "Doe",
                EmailConfirmed = true,
                IsActive = true,
                TenantId = tenant.Id
            };

            var result = await _userManager.CreateAsync(tenantAdmin, "TenantAdmin123!");
            if (result.Succeeded)
            {
                // Assign TenantAdmin role
                var tenantAdminRole = await _roleManager.FindByNameAsync("TenantAdmin");
                if (tenantAdminRole != null)
                {
                    var userTenantRole = new UserTenantRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = tenantAdmin.Id,
                        RoleId = tenantAdminRole.Id,
                        TenantId = tenant.Id,
                        IsActive = true
                    };

                    _context.UserTenantRoles.Add(userTenantRole);
                }

                // Update company contact person
                company.ContactPersonId = tenantAdmin.Id;
                await _context.SaveChangesAsync();

                // Create sample participant
                var participant = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "participant@acmecorp.com",
                    Email = "participant@acmecorp.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    EmailConfirmed = true,
                    IsActive = true,
                    TenantId = tenant.Id
                };

                var participantResult = await _userManager.CreateAsync(participant, "Participant123!");
                if (participantResult.Succeeded)
                {
                    // Assign Participant role
                    var participantRole = await _roleManager.FindByNameAsync("Participant");
                    if (participantRole != null)
                    {
                        var userParticipantRole = new UserTenantRole
                        {
                            Id = Guid.NewGuid(),
                            UserId = participant.Id,
                            RoleId = participantRole.Id,
                            TenantId = tenant.Id,
                            IsActive = true
                        };

                        _context.UserTenantRoles.Add(userParticipantRole);
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Created sample participant: {Email}", participant.Email);
                }

                _logger.LogInformation("Created sample tenant: {TenantSlug} with admin: {Email}", tenantSlug, tenantAdmin.Email);
            }
        }
    }
}
