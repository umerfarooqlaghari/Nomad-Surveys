using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class ClusterService : IClusterService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<ClusterService> _logger;

    public ClusterService(NomadSurveysDbContext context, ILogger<ClusterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ClusterListResponse>> GetClustersAsync(Guid? tenantId = null)
    {
        try
        {
            var query = _context.Clusters
                .Include(c => c.Competencies)
                .Where(c => c.IsActive) // Only return active (non-deleted) clusters
                .AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(c => c.TenantId == tenantId.Value);
            }

            var clusters = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return clusters.Select(c => new ClusterListResponse
            {
                Id = c.Id,
                ClusterName = c.ClusterName,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CompetencyCount = c.Competencies.Count(comp => comp.IsActive)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clusters for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<ClusterResponse?> GetClusterByIdAsync(Guid clusterId)
    {
        try
        {
            var cluster = await _context.Clusters
                .Include(c => c.Competencies.Where(comp => comp.IsActive))
                    .ThenInclude(comp => comp.Questions.Where(q => q.IsActive))
                .FirstOrDefaultAsync(c => c.Id == clusterId && c.IsActive); // Only return active clusters

            if (cluster == null)
            {
                return null;
            }

            return new ClusterResponse
            {
                Id = cluster.Id,
                ClusterName = cluster.ClusterName,
                Description = cluster.Description,
                IsActive = cluster.IsActive,
                CreatedAt = cluster.CreatedAt,
                UpdatedAt = cluster.UpdatedAt,
                TenantId = cluster.TenantId,
                Competencies = cluster.Competencies.Select(comp => new CompetencyResponse
                {
                    Id = comp.Id,
                    Name = comp.Name,
                    Description = comp.Description,
                    ClusterId = comp.ClusterId,
                    ClusterName = cluster.ClusterName,
                    IsActive = comp.IsActive,
                    CreatedAt = comp.CreatedAt,
                    UpdatedAt = comp.UpdatedAt,
                    TenantId = comp.TenantId,
                    Questions = comp.Questions.Select(q => new QuestionResponse
                    {
                        Id = q.Id,
                        CompetencyId = q.CompetencyId,
                        CompetencyName = comp.Name,
                        SelfQuestion = q.SelfQuestion,
                        OthersQuestion = q.OthersQuestion,
                        IsActive = q.IsActive,
                        CreatedAt = q.CreatedAt,
                        UpdatedAt = q.UpdatedAt,
                        TenantId = q.TenantId
                    }).ToList()
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cluster {ClusterId}", clusterId);
            throw;
        }
    }

    public async Task<ClusterResponse> CreateClusterAsync(CreateClusterRequest request, Guid tenantId)
    {
        try
        {
            var cluster = new Cluster
            {
                Id = Guid.NewGuid(),
                ClusterName = request.ClusterName,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            _context.Clusters.Add(cluster);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created cluster {ClusterId} for tenant {TenantId}", cluster.Id, tenantId);

            return new ClusterResponse
            {
                Id = cluster.Id,
                ClusterName = cluster.ClusterName,
                Description = cluster.Description,
                IsActive = cluster.IsActive,
                CreatedAt = cluster.CreatedAt,
                UpdatedAt = cluster.UpdatedAt,
                TenantId = cluster.TenantId,
                Competencies = new List<CompetencyResponse>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cluster for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<ClusterResponse?> UpdateClusterAsync(Guid clusterId, UpdateClusterRequest request)
    {
        try
        {
            var cluster = await _context.Clusters.FindAsync(clusterId);

            if (cluster == null)
            {
                return null;
            }

            cluster.ClusterName = request.ClusterName;
            cluster.Description = request.Description;
            cluster.UpdatedAt = DateTime.UtcNow;

            if (request.IsActive.HasValue)
            {
                cluster.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated cluster {ClusterId}", clusterId);

            return await GetClusterByIdAsync(clusterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cluster {ClusterId}", clusterId);
            throw;
        }
    }

    public async Task<bool> DeleteClusterAsync(Guid clusterId)
    {
        try
        {
            var cluster = await _context.Clusters.FindAsync(clusterId);

            if (cluster == null)
            {
                return false;
            }

            // Soft delete
            cluster.IsActive = false;
            cluster.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted (soft) cluster {ClusterId}", clusterId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cluster {ClusterId}", clusterId);
            throw;
        }
    }

    public async Task<bool> ClusterExistsAsync(Guid clusterId, Guid? tenantId = null)
    {
        try
        {
            var query = _context.Clusters
                .Where(c => c.Id == clusterId && c.IsActive); // Only check active clusters

            if (tenantId.HasValue)
            {
                query = query.Where(c => c.TenantId == tenantId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cluster {ClusterId} exists", clusterId);
            throw;
        }
    }
}

