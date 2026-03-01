using ClosedXML.Excel;
using Nomad.Api.DTOs.Report;
using Nomad.Api.Repository;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public interface IExcelReportService
{
    Task<(byte[] FileContent, string FileName)> GenerateRateeAverageExcelAsync(Guid surveyId, Guid tenantId);
    Task<(byte[] FileContent, string FileName)> GenerateSubjectWiseHeatMapExcelAsync(Guid surveyId, Guid tenantId);
    Task<(byte[] FileContent, string FileName)> GenerateSubjectWiseConsolidatedExcelAsync(Guid surveyId, Guid tenantId);
}

public class ExcelReportService : IExcelReportService
{
    private readonly IExcelReportRepository _repository;
    private readonly ILogger<ExcelReportService> _logger;

    public ExcelReportService(
        IExcelReportRepository repository,
        ILogger<ExcelReportService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(byte[] FileContent, string FileName)> GenerateRateeAverageExcelAsync(Guid surveyId, Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Generating Ratee Average Excel Report for Survey: {SurveyId}", surveyId);

            // 1. Fetch Data
            var reportData = await _repository.GetRateeAverageReportDataAsync(surveyId, tenantId);
            var clustersHierarchy = await _repository.GetClusterCompetencyHierarchyAsync(Guid.Empty, surveyId, tenantId);

            // 2. Prepare Worksheet
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ratee Average");

            // 3. Setup Headers
            int row = 1;
            int col = 5; // Starting column for Clusters/Competencies (A-D are fixed)
            int clusterIndex = 0;

            // Fixed Headers (A-D)
            worksheet.Cell(2, 1).Value = "Subject Employee ID";
            worksheet.Cell(2, 2).Value = "Subject's Full Name";
            worksheet.Cell(2, 3).Value = "Subject's Email Address";
            worksheet.Cell(2, 4).Value = "Subject's Department";

            // Dynamic Headers (Clusters & Competencies)
            // Iterate Cluster -> Competencies
            if (clustersHierarchy != null && clustersHierarchy.Clusters.Any())
            {
                foreach (var cluster in clustersHierarchy.Clusters)
                {
                    if (!cluster.Competencies.Any()) continue;

                    var clusterStartCol = col;
                    
                    foreach (var competency in cluster.Competencies)
                    {
                        // Row 2: Competency Headers
                        worksheet.Cell(2, col).Value = $"{competency.CompetencyName} (Self)";
                        worksheet.Cell(2, col + 1).Value = $"{competency.CompetencyName} (Others)";
                        
                        col += 2; // Move 2 columns for Self/Others
                    }

                    var clusterEndCol = col - 1;

                    // Row 1: Cluster Header (Merged)
                    var range = worksheet.Range(1, clusterStartCol, 1, clusterEndCol);
                    range.Merge();
                    range.Value = cluster.ClusterName;
                    range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    range.Style.Font.Bold = true;
                    // Alternating Cluster Colors: Light Pink and Peach
                    range.Style.Fill.BackgroundColor = (clusterIndex % 2 == 0) ? XLColor.LightPink : XLColor.PeachPuff;
                    
                    clusterIndex++;
                }
            }

            // Style Header Row (Row 2)
            var headerRange = worksheet.Range(2, 1, 2, col - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#B4C6E7");

            // 4. Fill Data
            row = 3;
            foreach (var item in reportData)
            {
                // Fixed Data
                worksheet.Cell(row, 1).Value = item.EmployeeId;
                worksheet.Cell(row, 2).Value = item.FullName;
                worksheet.Cell(row, 3).Value = item.Email;
                worksheet.Cell(row, 4).Value = item.Department;

                // Dynamic Data (aligned with headers)
                int dataCol = 5;
                if (clustersHierarchy != null && clustersHierarchy.Clusters.Any())
                {
                    foreach (var cluster in clustersHierarchy.Clusters)
                    {
                        foreach (var competency in cluster.Competencies)
                        {
                            var scores = item.CompetencyScores.GetValueOrDefault(competency.CompetencyName);
                            
                            // Self Score (handle null)
                             var selfCell = worksheet.Cell(row, dataCol);
                             if (scores.Self.HasValue) selfCell.Value = scores.Self.Value;
                             
                             // Others Score (handle null)
                             var othersCell = worksheet.Cell(row, dataCol + 1);
                             if (scores.Others.HasValue) othersCell.Value = scores.Others.Value;

                            dataCol += 2;
                        }
                    }
                }

                row++;
            }

            // Finish
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var contents = stream.ToArray();
            // Fetch details for filename
            var companyName = await _repository.GetSurveyAndTenantDetailsAsync(surveyId, tenantId);
            var fileName = $"{companyName} - Ratee Average.xlsx";

            return (contents, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Excel report");
            throw;
        }
    }

    public async Task<(byte[] FileContent, string FileName)> GenerateSubjectWiseHeatMapExcelAsync(Guid surveyId, Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Generating Subject Wise Heat Map Excel Report for Survey: {SurveyId}", surveyId);

            // 1. Fetch Data
            var reportData = await _repository.GetSubjectWiseHeatMapDataAsync(surveyId, tenantId);
            
            // 2. Prepare Worksheet
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Subject Wise Heat Map");

            // 3. Setup Headers
            
            // Fixed Headers
            worksheet.Cell(1, 1).Value = "Employee Id";
            worksheet.Cell(1, 2).Value = "Full Name";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Department";
            
            // Merge header 1 and 2 for fixed columns
            var fixedHeader1 = worksheet.Range(1, 1, 2, 1); fixedHeader1.Merge();
            var fixedHeader2 = worksheet.Range(1, 2, 2, 2); fixedHeader2.Merge();
            var fixedHeader3 = worksheet.Range(1, 3, 2, 3); fixedHeader3.Merge();
            var fixedHeader4 = worksheet.Range(1, 4, 2, 4); fixedHeader4.Merge();

            // Fixed range style
            var fixedHeader = worksheet.Range(1, 1, 2, 4);
            fixedHeader.Style.Font.Bold = true;
            fixedHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            fixedHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#B4C6E7"); 
            
            // Relationship Columns logic
            var relationships = new List<(string Header, string[] Keys)>
            {
                ("LINE MANAGER", new[] { "Line Manager", "Manager" }),
                ("Peer", new[] { "Peer" }),
                ("Self", new[] { "Self" }),
                ("STAKEHOLDER", new[] { "Stakeholder" }),
                ("DIRECT REPORTS", new[] { "Direct Reports", "Direct Report", "Direct" })
            };
            
            int col = 5;
            
            foreach (var rel in relationships)
            {
                // Merge 3 columns
                var rng = worksheet.Range(1, col, 1, col + 2);
                rng.Merge();
                rng.Value = rel.Header;
                rng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rng.Style.Font.Bold = true;
                rng.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2"); // Light blueish
                
                worksheet.Cell(2, col).Value = "Sent";
                worksheet.Cell(2, col + 1).Value = "Completed";
                worksheet.Cell(2, col + 2).Value = "Remaining";
                
                var subHeader = worksheet.Range(2, col, 2, col + 2);
                subHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                subHeader.Style.Font.Bold = true;
                subHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");

                col += 3;
            }
            
            // Grand Total
            var gtRng = worksheet.Range(1, col, 1, col + 2);
            gtRng.Merge();
            gtRng.Value = "Grand Total";
            gtRng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            gtRng.Style.Font.Bold = true;
            gtRng.Style.Fill.BackgroundColor = XLColor.FromHtml("#ACB9CA");

            worksheet.Cell(2, col).Value = "Surveys Sent";
            worksheet.Cell(2, col + 1).Value = "Surveys Completed";
            worksheet.Cell(2, col + 2).Value = "Surveys Remaining";
            
            var gtSubHeader = worksheet.Range(2, col, 2, col + 2);
            gtSubHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            gtSubHeader.Style.Font.Bold = true;
            gtSubHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#ACB9CA");
            
            // 4. Fill Data
            int row = 3;
            
             // Helper logic for red box
            bool ShouldMarkRed(string header, int completed)
            {
                 // Peer, Stakeholder, Direct Reports: 0 or 1 is Red.
                 if (string.Equals(header, "Peer", StringComparison.OrdinalIgnoreCase) || 
                     string.Equals(header, "STAKEHOLDER", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(header, "DIRECT REPORTS", StringComparison.OrdinalIgnoreCase))
                 {
                     return completed <= 1;
                 }
                 // Others: 0 is Red.
                 return completed == 0;
            }

            foreach (var item in reportData)
            {
                worksheet.Cell(row, 1).Value = item.EmployeeId;
                worksheet.Cell(row, 2).Value = item.FullName;
                worksheet.Cell(row, 3).Value = item.Email;
                worksheet.Cell(row, 4).Value = item.Department;
                
                int dataCol = 5;
                foreach (var rel in relationships)
                {
                    // Find matching key
                    var key = rel.Keys.FirstOrDefault(k => item.RelationshipData.ContainsKey(k));
                    var stats = key != null ? item.RelationshipData[key] : new RelationshipStats(); // 0s
                    
                    worksheet.Cell(row, dataCol).Value = stats.Sent;
                    var compCell = worksheet.Cell(row, dataCol + 1);
                    compCell.Value = stats.Completed;
                    
                    if (stats.Sent > 0 && ShouldMarkRed(rel.Header, stats.Completed))
                    {
                        compCell.Style.Fill.BackgroundColor = XLColor.LightSalmon; 
                    }
                    
                    worksheet.Cell(row, dataCol + 2).Value = stats.Remaining;
                    
                    dataCol += 3;
                }
                
                // Grand Total
                worksheet.Cell(row, dataCol).Value = item.GrandTotal.Sent;
                worksheet.Cell(row, dataCol + 1).Value = item.GrandTotal.Completed;
                worksheet.Cell(row, dataCol + 2).Value = item.GrandTotal.Remaining;
                
                row++;
            }
            
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var contents = stream.ToArray();
            
            var companyName = await _repository.GetSurveyAndTenantDetailsAsync(surveyId, tenantId);
            var fileName = $"{companyName} - Subject Wise Heat Map.xlsx";

            return (contents, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Subject Wise Heat Map Excel report");
            throw;
        }
    }

    public async Task<(byte[] FileContent, string FileName)> GenerateSubjectWiseConsolidatedExcelAsync(Guid surveyId, Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Generating Subject Wise Consolidated Excel Report for Survey: {SurveyId}", surveyId);

            // 1. Fetch Data
            var reportData = await _repository.GetSubjectWiseConsolidatedDataAsync(surveyId, tenantId);
            var questionsSchema = await _repository.GetQuestionColumnsAsync(surveyId, tenantId);
            var companyName = await _repository.GetSurveyAndTenantDetailsAsync(surveyId, tenantId);

            // Filter Schema
            var ratedQuestions = questionsSchema
                .Where(q => !string.Equals(q.QuestionType, "text", StringComparison.OrdinalIgnoreCase) && 
                            !string.Equals(q.QuestionType, "comment", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(q.QuestionType, "textarea", StringComparison.OrdinalIgnoreCase))
                .GroupBy(q => q.ClusterName) 
                .ToList();
                
            var openEndedQuestions = questionsSchema
                .Where(q => string.Equals(q.QuestionType, "text", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(q.QuestionType, "comment", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(q.QuestionType, "textarea", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 2. Prepare Worksheet
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Subject Consolidated");

            // 3. Setup Headers
            int col = 1;

            // --- Section 1: Metadata ---
            var metaHeaders = new[] { 
                "Evaluators employee id",
                "evaluators email",
                "evaluators name",
                "Subject's Employee Code", 
                "Subject's Email Address", 
                "Subject's Full Name", 
                "Subject Business Unit", 
                "Designation", 
                "Relationship Name"
            };
            
            for (int i = 0; i < metaHeaders.Length; i++)
            {
                worksheet.Cell(3, col + i).Value = metaHeaders[i];
            }
            
            var metaRange = worksheet.Range(3, 1, 3, col + metaHeaders.Length - 1);
            metaRange.Style.Font.Bold = true;
            metaRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#B4C6E7");
            metaRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            col += metaHeaders.Length;

            // --- Section 2: Rated Questions (Grouped by Cluster -> Competency) ---
            var competenciesGrouped = questionsSchema
                .Where(q => !string.Equals(q.QuestionType, "text", StringComparison.OrdinalIgnoreCase) && 
                            !string.Equals(q.QuestionType, "comment", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(q.QuestionType, "textarea", StringComparison.OrdinalIgnoreCase))
                .GroupBy(q => q.CompetencyName)
                .OrderBy(g => g.First().ClusterName)
                .ThenBy(g => g.Key)
                .ToList();
                
            int qIndex = 1;
            
            foreach (var compGroup in competenciesGrouped)
            {
                string competencyName = compGroup.Key;
                int startCol = col;
                
                foreach (var q in compGroup)
                {
                    // Row 2: Question Number
                    worksheet.Cell(2, col).Value = $"Q{qIndex}";
                    
                    // Row 3: Question Text
                    worksheet.Cell(3, col).Value = q.QuestionText;
                    
                    qIndex++;
                    col++;
                }
                
                int endCol = col - 1;
                
                // Row 1: Competency Header
                if (endCol >= startCol)
                {
                    var compRange = worksheet.Range(1, startCol, 1, endCol);
                    compRange.Merge();
                    compRange.Value = competencyName;
                    compRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    compRange.Style.Font.Bold = true;
                    compRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    compRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            // --- Section 3: Aggregated Scores (Cluster -> Competencies) ---
            var clustersGrouped = questionsSchema
                .Where(q => !string.Equals(q.QuestionType, "text", StringComparison.OrdinalIgnoreCase) && 
                            !string.Equals(q.QuestionType, "comment", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(q.QuestionType, "textarea", StringComparison.OrdinalIgnoreCase))
                .GroupBy(q => q.ClusterName)
                .OrderBy(g => g.Key)
                .ToList();
                
            foreach (var clusterGroup in clustersGrouped)
            {
                string clusterName = clusterGroup.Key;
                
                // 1. Cluster Score Column
                int clusterScoreCol = col;
                worksheet.Cell(1, col).Value = $"{clusterName} Score";
                worksheet.Cell(2, col).Value = ""; 
                worksheet.Cell(3, col).Value = ""; 
                
                var clRange = worksheet.Range(1, col, 3, col);
                clRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
                clRange.Style.Font.Bold = true;
                clRange.Style.Alignment.WrapText = true;
                
                col++;
                
                // 2. Competency Score Columns within this Cluster
                var compsInCluster = clusterGroup.Select(q => q.CompetencyName).Distinct().OrderBy(c => c).ToList();
                foreach (var compName in compsInCluster)
                {
                    worksheet.Cell(1, col).Value = $"{clusterName} {compName} Score";
                    worksheet.Cell(2, col).Value = "";
                    worksheet.Cell(3, col).Value = compName;
                    
                    worksheet.Cell(1, col).Style.Alignment.WrapText = true;
                    worksheet.Cell(3, col).Style.Font.Bold = true;
                    
                    col++;
                }
            }

            // --- Section 4: Open Ended Feedback ---
            if (openEndedQuestions.Any())
            {
                int startOpenCol = col;
                foreach (var q in openEndedQuestions)
                {
                    worksheet.Cell(2, col).Value = "Open Ended";
                    worksheet.Cell(3, col).Value = q.QuestionText;
                    col++;
                }
                var openRange = worksheet.Range(1, startOpenCol, 1, col - 1);
                openRange.Merge();
                openRange.Value = "Open Ended Feedback";
                openRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                openRange.Style.Font.Bold = true;
            }
            
            // --- Section 5: Total Score ---
            worksheet.Cell(1, col).Value = "Self Evaluation Aggregated Score";
            var totalRange = worksheet.Range(1, col, 3, col);
            totalRange.Merge();
            totalRange.Style.Alignment.WrapText = true;
            totalRange.Style.Font.Bold = true;
            
            // 4. Fill Data
            int row = 4;
            foreach (var item in reportData)
            {
                int dataCol = 1;
                
                // Metadata
                worksheet.Cell(row, dataCol++).Value = item.EvaluatorEmployeeId;
                worksheet.Cell(row, dataCol++).Value = item.EvaluatorEmail;
                worksheet.Cell(row, dataCol++).Value = item.EvaluatorFullName;
                worksheet.Cell(row, dataCol++).Value = item.EmployeeId;
                worksheet.Cell(row, dataCol++).Value = item.Email;
                worksheet.Cell(row, dataCol++).Value = item.FullName;
                worksheet.Cell(row, dataCol++).Value = item.BusinessUnit;
                worksheet.Cell(row, dataCol++).Value = item.Designation;
                worksheet.Cell(row, dataCol++).Value = item.Relationship;
                
                // Rated Questions
                foreach (var compGroup in competenciesGrouped)
                {
                    foreach (var q in compGroup)
                    {
                        if (item.QuestionScores.TryGetValue(q.QuestionId, out double score))
                        {
                            worksheet.Cell(row, dataCol).Value = score;
                        }
                        dataCol++;
                    }
                }
                
                // Aggregated Scores
                foreach (var clusterGroup in clustersGrouped)
                {
                    // Cluster Score
                    if (item.ClusterScores.TryGetValue(clusterGroup.Key, out double cScore))
                    {
                        worksheet.Cell(row, dataCol).Value = cScore;
                    }
                    dataCol++;
                    
                    // Competency Scores
                    var compsInCluster = clusterGroup.Select(q => q.CompetencyName).Distinct().OrderBy(c => c).ToList();
                    foreach (var compName in compsInCluster)
                    {
                        if (item.CompetencyScores.TryGetValue(compName, out double compScore))
                        {
                            worksheet.Cell(row, dataCol).Value = compScore;
                        }
                        dataCol++;
                    }
                }
                
                // Open Ended
                foreach (var q in openEndedQuestions)
                {
                    if (item.OpenEndedResponses.TryGetValue(q.QuestionId, out string answer))
                    {
                        worksheet.Cell(row, dataCol).Value = answer;
                        worksheet.Cell(row, dataCol).Style.Alignment.WrapText = true;
                    }
                    dataCol++;
                }
                
                // Total Score
                worksheet.Cell(row, dataCol).Value = item.TotalScore;
                
                row++;
            }

            // Finish
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var contents = stream.ToArray();
            
            var fileName = $"{companyName} - 360 survey Results.xlsx";

            return (contents, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Subject Wise Consolidated Excel report");
            throw;
        }
    }
}
