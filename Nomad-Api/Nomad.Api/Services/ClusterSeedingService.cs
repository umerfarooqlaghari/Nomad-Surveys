using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;
using static Nomad.Api.Data.ClusterDataBank;

namespace Nomad.Api.Services;

public class ClusterSeedingService : IClusterSeedingService
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<ClusterSeedingService> _logger;
    private readonly IWebHostEnvironment _environment;

    public ClusterSeedingService(
        NomadSurveysDbContext context,
        ILogger<ClusterSeedingService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    public async Task SeedClustersAsync(Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Starting cluster seeding for tenant {TenantId} from static ClusterDataBank", tenantId);

            // Get existing clusters for this tenant to ensure idempotency
            var existingClusters = await _context.Clusters
                .Where(c => c.TenantId == tenantId)
                .Include(c => c.Competencies)
                .ToListAsync();

            foreach (var clusterDef in ClusterDataBank.Clusters)
            {
                // Check if cluster exists
                var cluster = existingClusters.FirstOrDefault(c => c.ClusterName.Equals(clusterDef.Name, StringComparison.OrdinalIgnoreCase));

                if (cluster == null)
                {
                    cluster = new Cluster
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ClusterName = clusterDef.Name,
                        Description = clusterDef.Description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Clusters.Add(cluster);
                    existingClusters.Add(cluster);
                }

                foreach (var competencyDef in clusterDef.Competencies)
                {
                    // Check if competency exists in this cluster
                    var competency = cluster.Competencies.FirstOrDefault(c => c.Name.Equals(competencyDef.Name, StringComparison.OrdinalIgnoreCase));

                    if (competency == null)
                    {
                        competency = new Competency
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            ClusterId = cluster.Id,
                            Name = competencyDef.Name,
                            Description = competencyDef.Description,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Competencies.Add(competency);
                    }

                    // For questions, check if we need to add them
                    bool isNewCompetency = _context.Entry(competency).State == EntityState.Added;

                    if (!isNewCompetency)
                    {
                         await _context.Entry(competency).Collection(c => c.Questions).LoadAsync();
                    }

                    foreach (var qDef in competencyDef.Questions)
                    {
                        // Avoid adding duplicate question text
                        if (!isNewCompetency && competency.Questions.Any(eq => eq.OthersQuestion == qDef.OthersQuestion && eq.SelfQuestion == qDef.SelfQuestion))
                        {
                            continue;
                        }

                        var question = new Question
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            CompetencyId = competency.Id,
                            SelfQuestion = qDef.SelfQuestion,
                            OthersQuestion = qDef.OthersQuestion,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Questions.Add(question);
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully seeded clusters for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding clusters for tenant {TenantId}", tenantId);
            throw; // Re-throw to be caught by the caller logic if needed, or handle here. 
                   // Given caller wraps this, re-throw ensures caller knows it failed? 
                   // Actually caller logs. Let's just throw.
        }
    }
}
