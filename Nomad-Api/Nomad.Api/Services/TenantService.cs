using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class TenantService : ITenantService
{
    private readonly NomadSurveysDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        NomadSurveysDbContext context,
        IMapper mapper,
        IAuthenticationService authenticationService,
        ILogger<TenantService> logger)
    {
        _context = context;
        _mapper = mapper;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<TenantResponse> CreateTenantAsync(CreateTenantRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Check if slug already exists
            var existingTenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == request.Slug);

            if (existingTenant != null)
            {
                throw new InvalidOperationException($"Tenant with slug '{request.Slug}' already exists");
            }

            // Create tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Create company
            var company = new Company
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = request.Company.Name,
                NumberOfEmployees = request.Company.NumberOfEmployees,
                Location = request.Company.Location,
                Industry = request.Company.Industry,
                ContactPersonName = request.Company.ContactPersonName,
                ContactPersonEmail = request.Company.ContactPersonEmail,
                ContactPersonRole = request.Company.ContactPersonRole,
                ContactPersonPhone = request.Company.ContactPersonPhone,
                CreatedAt = DateTime.UtcNow
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Create tenant admin user
            var createUserRequest = new CreateUserRequest
            {
                FirstName = request.TenantAdmin.FirstName,
                LastName = request.TenantAdmin.LastName,
                Email = request.TenantAdmin.Email,
                PhoneNumber = request.TenantAdmin.PhoneNumber,
                Password = request.TenantAdmin.Password,
                Roles = new List<string> { "TenantAdmin" }
            };

            var tenantAdmin = await _authenticationService.CreateUserAsync(createUserRequest, tenant.Id);

            // Update company with contact person
            company.ContactPersonId = tenantAdmin.Id;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Load tenant with company for response
            var createdTenant = await _context.Tenants
                .IgnoreQueryFilters()
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.Id == tenant.Id);

            var response = _mapper.Map<TenantResponse>(createdTenant);
            
            _logger.LogInformation("Tenant created successfully: {TenantId} with slug {TenantSlug}", tenant.Id, tenant.Slug);
            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create tenant with slug {TenantSlug}", request.Slug);
            throw;
        }
    }

    public async Task<TenantResponse?> GetTenantByIdAsync(Guid tenantId)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .Include(t => t.Company)
                .ThenInclude(c => c!.ContactPerson)
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            return tenant != null ? _mapper.Map<TenantResponse>(tenant) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<TenantResponse?> GetTenantBySlugAsync(string slug)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .Include(t => t.Company)
                .ThenInclude(c => c!.ContactPerson)
                .FirstOrDefaultAsync(t => t.Slug == slug);

            return tenant != null ? _mapper.Map<TenantResponse>(tenant) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant by slug {TenantSlug}", slug);
            return null;
        }
    }

    public async Task<List<TenantListResponse>> GetTenantsAsync()
    {
        try
        {
            var tenants = await _context.Tenants
                .IgnoreQueryFilters()
                .Include(t => t.Company)
                .Include(t => t.Users)
                .ToListAsync();

            var tenantResponses = tenants.Select(t => new TenantListResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UserCount = t.Users.Count(u => u.IsActive),
                CompanyName = t.Company?.Name
            }).ToList();

            return tenantResponses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants list");
            return new List<TenantListResponse>();
        }
    }

    public async Task<bool> UpdateTenantAsync(Guid tenantId, CreateTenantRequest request)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
            {
                return false;
            }

            // Check if new slug conflicts with existing tenant
            if (tenant.Slug != request.Slug)
            {
                var existingTenant = await _context.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == request.Slug && t.Id != tenantId);

                if (existingTenant != null)
                {
                    throw new InvalidOperationException($"Tenant with slug '{request.Slug}' already exists");
                }
            }

            tenant.Name = request.Name;
            tenant.Slug = request.Slug;
            tenant.Description = request.Description;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tenant {TenantId} updated successfully", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> DeactivateTenantAsync(Guid tenantId)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
            {
                return false;
            }

            tenant.IsActive = false;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tenant {TenantId} deactivated successfully", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> ActivateTenantAsync(Guid tenantId)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
            {
                return false;
            }

            tenant.IsActive = true;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tenant {TenantId} activated successfully", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<CompanyResponse?> GetCompanyByTenantIdAsync(Guid tenantId)
    {
        try
        {
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .Include(c => c.ContactPerson)
                .Include(c => c.Tenant)
                .FirstOrDefaultAsync(c => c.TenantId == tenantId);

            return company != null ? _mapper.Map<CompanyResponse>(company) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> UpdateCompanyAsync(Guid tenantId, CreateCompanyRequest request)
    {
        try
        {
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId);

            if (company == null)
            {
                return false;
            }

            company.Name = request.Name;
            company.NumberOfEmployees = request.NumberOfEmployees;
            company.Location = request.Location;
            company.Industry = request.Industry;
            company.ContactPersonName = request.ContactPersonName;
            company.ContactPersonEmail = request.ContactPersonEmail;
            company.ContactPersonRole = request.ContactPersonRole;
            company.ContactPersonPhone = request.ContactPersonPhone;
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Company for tenant {TenantId} updated successfully", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company for tenant {TenantId}", tenantId);
            return false;
        }
    }
}
