namespace Nomad.Api.Services.Interfaces;

public interface IClusterSeedingService
{
    Task SeedClustersAsync(Guid tenantId);
}
