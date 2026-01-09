using ClosedXML.Excel;
using Nomad.Api.Repository;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public interface IExcelReportService
{
    Task<(byte[] FileContent, string FileName)> GenerateRateeAverageExcelAsync(Guid surveyId, Guid tenantId);
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
                }
            }

            // Style Header Row (Row 2)
            var headerRange = worksheet.Range(2, 1, 2, col - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

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
            var content = stream.ToArray();

            // Default filename, controller can override with Company Name + Year
            return (content, "Ratee Average Report.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Excel report");
            throw;
        }
    }
}
