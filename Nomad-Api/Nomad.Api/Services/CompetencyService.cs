using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class CompetencyService : ICompetencyService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<CompetencyService> _logger;

    public CompetencyService(NomadSurveysDbContext context, ILogger<CompetencyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CompetencyListResponse>> GetCompetenciesAsync(Guid? tenantId = null, Guid? clusterId = null)
    {
        try
        {
            var query = _context.Competencies
                .Include(c => c.Cluster)
                .Include(c => c.Questions)
                .Where(c => c.IsActive) // Only return active (non-deleted) competencies
                .AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(c => c.TenantId == tenantId.Value);
            }

            if (clusterId.HasValue)
            {
                query = query.Where(c => c.ClusterId == clusterId.Value);
            }

            var competencies = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return competencies.Select(c => new CompetencyListResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ClusterId = c.ClusterId,
                ClusterName = c.Cluster?.ClusterName,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                QuestionCount = c.Questions.Count(q => q.IsActive)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving competencies for tenant {TenantId}, cluster {ClusterId}", tenantId, clusterId);
            throw;
        }
    }

    public async Task<CompetencyResponse?> GetCompetencyByIdAsync(Guid competencyId)
    {
        try
        {
            var competency = await _context.Competencies
                .Include(c => c.Cluster)
                .Include(c => c.Questions.Where(q => q.IsActive))
                .FirstOrDefaultAsync(c => c.Id == competencyId && c.IsActive); // Only return active competencies

            if (competency == null)
            {
                return null;
            }

            return new CompetencyResponse
            {
                Id = competency.Id,
                Name = competency.Name,
                Description = competency.Description,
                ClusterId = competency.ClusterId,
                ClusterName = competency.Cluster?.ClusterName,
                IsActive = competency.IsActive,
                CreatedAt = competency.CreatedAt,
                UpdatedAt = competency.UpdatedAt,
                TenantId = competency.TenantId,
                Questions = competency.Questions.Select(q => new QuestionResponse
                {
                    Id = q.Id,
                    CompetencyId = q.CompetencyId,
                    CompetencyName = competency.Name,
                    SelfQuestion = q.SelfQuestion,
                    OthersQuestion = q.OthersQuestion,
                    IsActive = q.IsActive,
                    CreatedAt = q.CreatedAt,
                    UpdatedAt = q.UpdatedAt,
                    TenantId = q.TenantId
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving competency {CompetencyId}", competencyId);
            throw;
        }
    }

    public async Task<CompetencyResponse> CreateCompetencyAsync(CreateCompetencyRequest request, Guid tenantId)
    {
        try
        {
            // Verify cluster exists and belongs to the same tenant
            var cluster = await _context.Clusters
                .FirstOrDefaultAsync(c => c.Id == request.ClusterId && c.TenantId == tenantId);

            if (cluster == null)
            {
                throw new InvalidOperationException($"Cluster {request.ClusterId} not found or does not belong to tenant {tenantId}");
            }

            var competency = new Competency
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ClusterId = request.ClusterId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.Competencies.Add(competency);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created competency {CompetencyId} for cluster {ClusterId}, tenant {TenantId}", 
                competency.Id, request.ClusterId, tenantId);

            return new CompetencyResponse
            {
                Id = competency.Id,
                Name = competency.Name,
                Description = competency.Description,
                ClusterId = competency.ClusterId,
                ClusterName = cluster.ClusterName,
                IsActive = competency.IsActive,
                CreatedAt = competency.CreatedAt,
                UpdatedAt = competency.UpdatedAt,
                TenantId = competency.TenantId,
                Questions = new List<QuestionResponse>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating competency for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<CompetencyResponse?> UpdateCompetencyAsync(Guid competencyId, UpdateCompetencyRequest request)
    {
        try
        {
            var competency = await _context.Competencies
                .Include(c => c.Tenant)
                .FirstOrDefaultAsync(c => c.Id == competencyId);

            if (competency == null)
            {
                return null;
            }

            // Verify cluster exists and belongs to the same tenant
            var cluster = await _context.Clusters
                .FirstOrDefaultAsync(c => c.Id == request.ClusterId && c.TenantId == competency.TenantId);

            if (cluster == null)
            {
                throw new InvalidOperationException($"Cluster {request.ClusterId} not found or does not belong to the same tenant");
            }

            competency.Name = request.Name;
            competency.Description = request.Description;
            competency.ClusterId = request.ClusterId;
            competency.UpdatedAt = DateTime.UtcNow;

            if (request.IsActive.HasValue)
            {
                competency.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated competency {CompetencyId}", competencyId);

            return await GetCompetencyByIdAsync(competencyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating competency {CompetencyId}", competencyId);
            throw;
        }
    }

    public async Task<bool> DeleteCompetencyAsync(Guid competencyId)
    {
        try
        {
            var competency = await _context.Competencies.FindAsync(competencyId);

            if (competency == null)
            {
                return false;
            }

            // Soft delete
            competency.IsActive = false;
            competency.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted (soft) competency {CompetencyId}", competencyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting competency {CompetencyId}", competencyId);
            throw;
        }
    }

    public async Task<bool> CompetencyExistsAsync(Guid competencyId, Guid? tenantId = null)
    {
        try
        {
            var query = _context.Competencies
                .Where(c => c.Id == competencyId && c.IsActive); // Only check active competencies

            if (tenantId.HasValue)
            {
                query = query.Where(c => c.TenantId == tenantId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if competency {CompetencyId} exists", competencyId);
            throw;
        }
    }
}

