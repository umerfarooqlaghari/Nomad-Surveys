namespace Nomad.Api.DTOs.Report;

public class SubjectWiseHeatMapItem
{
    public Guid SubjectId { get; set; }
    public string? EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }

    // Key: Relationship Name (Normalized), Value: Stats
    public Dictionary<string, RelationshipStats> RelationshipData { get; set; } = new();

    public RelationshipStats GrandTotal { get; set; } = new();
}

public class RelationshipStats
{
    public int Sent { get; set; }
    public int Completed { get; set; }
    public int Remaining => Sent - Completed;
}
