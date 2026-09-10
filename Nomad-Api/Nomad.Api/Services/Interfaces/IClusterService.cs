using Alpha.Api.DTOs.Request;
using Alpha.Api.DTOs.Response;

namespace Alpha.Api.Services.Interfaces;

public interface IClusterService
{
    Task<List<ClusterListResponse>> GetClustersAsync(Guid? tenantId = null);
    Task<ClusterResponse?> GetClusterByIdAsync(Guid clusterId);
    Task<ClusterResponse> CreateClusterAsync(CreateClusterRequest request, Guid tenantId);
    Task<ClusterResponse?> UpdateClusterAsync(Guid clusterId, UpdateClusterRequest request);
    Task<bool> DeleteClusterAsync(Guid clusterId);
    Task<bool> ClusterExistsAsync(Guid clusterId, Guid? tenantId = null);
}

