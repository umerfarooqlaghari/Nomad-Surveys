using MiniExcelLibs;
using System.Text;

var filePath = "ClusterBank.xlsx";

if (!File.Exists(filePath))
{
    Console.WriteLine("File not found: " + filePath);
    return;
}

var rows = MiniExcel.Query(filePath).ToList();
var clusterMap = new Dictionary<string, Dictionary<string, List<(string Self, string Others)>>>();

foreach (var row in rows)
{
    var rowDict = (IDictionary<string, object>)row;
    // Debug: print keys for first row
    if (rows.IndexOf(row) == 0)
    {
        Console.WriteLine("Columns found: " + string.Join(", ", rowDict.Keys));
    }
    // Map based on column indices/names reported: A, B, C, D
    // Assuming: 
    // A: Cluster
    // B: Competency
    // C: Self Question
    // D: Others Question (or vice versa? Let's check typical structure)
    // Actually, usually headers are in first row. If MiniExcel returned A,B,C, it treated first row as data.
    // I should check if the first row contains "Cluster" string to confirm it's a header.
    
    // Let's assume the first row IS the header if it contains text like "Cluster".
    var colA = GetValue(rowDict, "A");
    var colB = GetValue(rowDict, "B");
    
    if (colA != null && colA.Contains("Cluster", StringComparison.OrdinalIgnoreCase)) continue; // Skip header row if it is one

    string clusterName = GetValue(rowDict, "A", "Cluster");
    string competencyName = GetValue(rowDict, "B", "Competency");
    string selfQuestion = GetValue(rowDict, "C", "SelfQuestion", "Self");
    string othersQuestion = GetValue(rowDict, "D", "OthersQuestion", "Others");

    if (string.IsNullOrWhiteSpace(clusterName) || string.IsNullOrWhiteSpace(competencyName)) continue;

    if (!clusterMap.ContainsKey(clusterName)) clusterMap[clusterName] = new Dictionary<string, List<(string, string)>>();
    if (!clusterMap[clusterName].ContainsKey(competencyName)) clusterMap[clusterName][competencyName] = new List<(string, string)>();
    
    if (!string.IsNullOrWhiteSpace(selfQuestion) && !string.IsNullOrWhiteSpace(othersQuestion))
    {
        clusterMap[clusterName][competencyName].Add((selfQuestion, othersQuestion));
    }
}

// Generate C# Code
var sb = new StringBuilder();
sb.AppendLine("using System.Collections.Generic;");
sb.AppendLine("namespace Nomad.Api.Data;");
sb.AppendLine();
sb.AppendLine("public static class ClusterDataBank");
sb.AppendLine("{");
sb.AppendLine("    public static readonly List<ClusterDefinition> Clusters = new List<ClusterDefinition>");
sb.AppendLine("    {");

foreach (var cluster in clusterMap)
{
    sb.AppendLine($"        new ClusterDefinition");
    sb.AppendLine("        {");
    sb.AppendLine($"            Name = \"{Escape(cluster.Key)}\",");
    sb.AppendLine($"            Description = \"{Escape(cluster.Key)} Cluster\",");
    sb.AppendLine("            Competencies = new List<CompetencyDefinition>");
    sb.AppendLine("            {");
    foreach (var comp in cluster.Value)
    {
        sb.AppendLine("                new CompetencyDefinition");
        sb.AppendLine("                {");
        sb.AppendLine($"                    Name = \"{Escape(comp.Key)}\",");
        sb.AppendLine($"                    Description = \"{Escape(comp.Key)} Competency\",");
        sb.AppendLine("                    Questions = new List<QuestionDefinition>");
        sb.AppendLine("                    {");
        foreach (var q in comp.Value)
        {
            sb.AppendLine("                        new QuestionDefinition");
            sb.AppendLine("                        {");
            sb.AppendLine($"                            SelfQuestion = \"{Escape(q.Self)}\",");
            sb.AppendLine($"                            OthersQuestion = \"{Escape(q.Others)}\"");
            sb.AppendLine("                        },");
        }
        sb.AppendLine("                    }");
        sb.AppendLine("                },");
    }
    sb.AppendLine("            }");
    sb.AppendLine("        },");
}

sb.AppendLine("    };");
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine("public class ClusterDefinition");
sb.AppendLine("{");
sb.AppendLine("    public string Name { get; set; } = string.Empty;");
sb.AppendLine("    public string Description { get; set; } = string.Empty;");
sb.AppendLine("    public List<CompetencyDefinition> Competencies { get; set; } = new();");
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine("public class CompetencyDefinition");
sb.AppendLine("{");
sb.AppendLine("    public string Name { get; set; } = string.Empty;");
sb.AppendLine("    public string Description { get; set; } = string.Empty;");
sb.AppendLine("    public List<QuestionDefinition> Questions { get; set; } = new();");
sb.AppendLine("}");
sb.AppendLine();
sb.AppendLine("public class QuestionDefinition");
sb.AppendLine("{");
sb.AppendLine("    public string SelfQuestion { get; set; } = string.Empty;");
sb.AppendLine("    public string OthersQuestion { get; set; } = string.Empty;");
sb.AppendLine("}");


File.WriteAllText("ClusterDataBank.cs", sb.ToString());
Console.WriteLine("File ClusterDataBank.cs generated successfully.");

string GetValue(IDictionary<string, object> row, params string[] keys)
{
    foreach (var key in keys)
    {
        var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        if (match != null && row[match] != null) return row[match].ToString()?.Trim() ?? string.Empty;
    }
    return string.Empty;
}

string Escape(string input)
{
    if (string.IsNullOrEmpty(input)) return "";
    // Remove newlines and escape quotes
    return input.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
}
