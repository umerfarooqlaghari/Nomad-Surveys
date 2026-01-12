namespace Nomad.Api.DTOs.Report;

/// <summary>
/// Result for hierarchical cluster-competency summary (Part 2 dynamic pages)
/// </summary>
public class ClusterCompetencyHierarchyResult
{
    public List<ClusterSummaryItem> Clusters { get; set; } = new();
    public List<string> RelationshipTypes { get; set; } = new();
}

/// <summary>
/// Cluster-level summary with its competencies
/// </summary>
public class ClusterSummaryItem
{
    public string ClusterName { get; set; } = string.Empty;
    public double? SelfScore { get; set; }
    public double? OthersScore { get; set; }
    public Dictionary<string, double?> RelationshipScores { get; set; } = new();
    /// <summary>
    /// Competencies within this cluster (for Dimensions Summary table on cluster page)
    /// </summary>
    public List<CompetencySummaryItem> Competencies { get; set; } = new();
}

/// <summary>
/// Competency-level summary with its questions/items
/// </summary>
public class CompetencySummaryItem
{
    public string CompetencyName { get; set; } = string.Empty;
    public string ClusterName { get; set; } = string.Empty;
    public double? SelfScore { get; set; }
    public double? OthersScore { get; set; }
    public Dictionary<string, double?> RelationshipScores { get; set; } = new();
    /// <summary>
    /// Questions/items within this competency (for Item Level Feedback table on competency page)
    /// </summary>
    public List<QuestionSummaryItem> Questions { get; set; } = new();
}

/// <summary>
/// Question/item-level summary
/// </summary>
public class QuestionSummaryItem
{
    public string QuestionText { get; set; } = string.Empty;
    public double? SelfScore { get; set; }
    public double? OthersScore { get; set; }
    public Dictionary<string, double?> RelationshipScores { get; set; } = new();
}
