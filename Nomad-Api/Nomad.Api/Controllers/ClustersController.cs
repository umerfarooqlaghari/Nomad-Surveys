using Microsoft.AspNetCore.Mvc;
using Nomad.Api.Authorization;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
[AuthorizeTenant]
public class ClustersController : ControllerBase
{
    private readonly IClusterService _clusterService;
    private readonly ILogger<ClustersController> _logger;

    public ClustersController(
        IClusterService clusterService,
        ILogger<ClustersController> logger)
    {
        _clusterService = clusterService;
        _logger = logger;
    }

    private Guid? GetCurrentTenantId() => HttpContext.Items["TenantId"] as Guid?;

    /// <summary>
    /// Get all clusters for the current tenant
    /// </summary>
    /// <returns>List of clusters</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ClusterListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ClusterListResponse>>> GetClusters()
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var clusters = await _clusterService.GetClustersAsync(tenantId);
            return Ok(clusters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clusters");
            return StatusCode(500, new { error = "An error occurred while retrieving clusters" });
        }
    }

    /// <summary>
    /// Get a specific cluster by ID
    /// </summary>
    /// <param name="id">Cluster ID</param>
    /// <returns>Cluster details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClusterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClusterResponse>> GetClusterById(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var cluster = await _clusterService.GetClusterByIdAsync(id);
            
            if (cluster == null)
            {
                return NotFound(new { error = $"Cluster with ID {id} not found" });
            }

            // Verify the cluster belongs to the current tenant
            if (cluster.TenantId != tenantId)
            {
                return NotFound(new { error = $"Cluster with ID {id} not found" });
            }

            return Ok(cluster);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cluster {ClusterId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the cluster" });
        }
    }

    /// <summary>
    /// Create a new cluster
    /// </summary>
    /// <param name="request">Cluster creation request</param>
    /// <returns>Created cluster</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ClusterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClusterResponse>> CreateCluster([FromBody] CreateClusterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            var cluster = await _clusterService.CreateClusterAsync(request, tenantId.Value);

            // Return 201 Created with the created cluster
            // Note: CreatedAtAction fails with tenant-scoped routes, so we return Created with a simple status
            return StatusCode(StatusCodes.Status201Created, cluster);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cluster");
            return StatusCode(500, new { error = "An error occurred while creating the cluster" });
        }
    }

    /// <summary>
    /// Update an existing cluster
    /// </summary>
    /// <param name="id">Cluster ID</param>
    /// <param name="request">Cluster update request</param>
    /// <returns>Updated cluster</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClusterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClusterResponse>> UpdateCluster(Guid id, [FromBody] UpdateClusterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify cluster exists and belongs to tenant
            var existingCluster = await _clusterService.GetClusterByIdAsync(id);
            if (existingCluster == null || existingCluster.TenantId != tenantId)
            {
                return NotFound(new { error = $"Cluster with ID {id} not found" });
            }

            var cluster = await _clusterService.UpdateClusterAsync(id, request);
            
            if (cluster == null)
            {
                return NotFound(new { error = $"Cluster with ID {id} not found" });
            }

            return Ok(cluster);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cluster {ClusterId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the cluster" });
        }
    }

    /// <summary>
    /// Delete a cluster (soft delete)
    /// </summary>
    /// <param name="id">Cluster ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCluster(Guid id)
    {
        try
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { error = "Tenant context not found" });
            }

            // Verify cluster exists and belongs to tenant
            var existingCluster = await _clusterService.GetClusterByIdAsync(id);
            if (existingCluster == null || existingCluster.TenantId != tenantId)
            {
                return NotFound(new { error = $"Cluster with ID {id} not found" });
            }

            var result = await _clusterService.DeleteClusterAsync(id);
            
            if (!result)
            {
                return NotFound(new { error = $"Cluster with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cluster {ClusterId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the cluster" });
        }
    }
}

