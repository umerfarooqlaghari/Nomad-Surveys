using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;

namespace Nomad.Api.Services.Interfaces;

public interface IClusterService
{
    Task<List<ClusterListResponse>> GetClustersAsync(Guid? tenantId = null);
    Task<ClusterResponse?> GetClusterByIdAsync(Guid clusterId);
    Task<ClusterResponse> CreateClusterAsync(CreateClusterRequest request, Guid tenantId);
    Task<ClusterResponse?> UpdateClusterAsync(Guid clusterId, UpdateClusterRequest request);
    Task<bool> DeleteClusterAsync(Guid clusterId);
    Task<bool> ClusterExistsAsync(Guid clusterId, Guid? tenantId = null);
}

