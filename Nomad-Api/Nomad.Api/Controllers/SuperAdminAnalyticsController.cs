using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Authorization;
using Nomad.Api.Data;

namespace Nomad.Api.Controllers;

/// <summary>
/// Controller for SuperAdmin analytics and dashboard data
/// </summary>
[ApiController]
[Route("api/superadmin/analytics")]
[AuthorizeSuperAdmin]
public class SuperAdminAnalyticsController : ControllerBase
{
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<SuperAdminAnalyticsController> _logger;

    public SuperAdminAnalyticsController(
        NomadSurveysDbContext context,
        ILogger<SuperAdminAnalyticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview statistics
    /// </summary>
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewResponse>> GetOverview()
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);

            // Get companies count (tenants with companies)
            var totalCompanies = await _context.Companies.CountAsync();
            var companiesLastMonth = await _context.Companies
                .Where(c => c.CreatedAt < startOfCurrentMonth)
                .Where(c => c.Tenant.IsActive)
                .CountAsync();
            
            // Get users count (excluding SuperAdmin users - those without TenantId)
            var totalUsers = await _context.Users.Where(u => u.TenantId != null).Where(c => c.Tenant.IsActive).CountAsync();
            var usersLastMonth = await _context.Users
                .Where(u => u.TenantId != null && u.CreatedAt < startOfCurrentMonth)
                .Where(c => c.Tenant.IsActive)
                .CountAsync();
            
            // Get completed surveys count
            var completedSurveys = await _context.SurveySubmissions
                .Where(s => s.Status == "Completed")
                .CountAsync();
            var completedSurveysLastMonth = await _context.SurveySubmissions
                .Where(s => s.Status == "Completed" && s.CompletedAt < startOfCurrentMonth)
                .Where(c => c.Tenant.IsActive)
                .CountAsync();
            
            // Get survey completion rate
            var totalSurveySubmissions = await _context.SurveySubmissions.Where(c => c.Tenant.IsActive).CountAsync();
            var completionRate = totalSurveySubmissions > 0 
                ? Math.Round((double)completedSurveys / totalSurveySubmissions * 100, 1)
                : 0;
            
            var totalSubmissionsLastMonth = await _context.SurveySubmissions
                .Where(s => s.CreatedAt < startOfCurrentMonth)
                .Where(c => c.Tenant.IsActive)
                .CountAsync();
            var completedSubmissionsLastMonth = await _context.SurveySubmissions
                .Where(s => s.Status == "Completed" && s.CreatedAt < startOfCurrentMonth)
                .Where(c => c.Tenant.IsActive)
                .CountAsync();
            var completionRateLastMonth = totalSubmissionsLastMonth > 0 
                ? Math.Round((double)completedSubmissionsLastMonth / totalSubmissionsLastMonth * 100, 1)
                : 0;

            // Calculate percentage changes
            var companiesChange = CalculatePercentageChange(companiesLastMonth, totalCompanies);
            var usersChange = CalculatePercentageChange(usersLastMonth, totalUsers);
            var surveysChange = CalculatePercentageChange(completedSurveysLastMonth, completedSurveys);
            var completionRateChange = Math.Round(completionRate - completionRateLastMonth, 1);

            // Get user growth data (last 6 months)
            var userGrowthData = await GetUserGrowthData(6);
            
            // Get survey completion trends (last 6 months)
            var surveyTrendsData = await GetSurveyCompletionTrends(6);

            return Ok(new DashboardOverviewResponse
            {
                CompaniesRegistered = new StatItem
                {
                    Value = totalCompanies,
                    Change = companiesChange,
                    ChangeType = companiesChange >= 0 ? "increase" : "decrease"
                },
                UsersRegistered = new StatItem
                {
                    Value = totalUsers,
                    Change = Math.Abs(usersChange),
                    ChangeType = usersChange >= 0 ? "increase" : "decrease"
                },
                SurveysCompleted = new StatItem
                {
                    Value = completedSurveys,
                    Change = Math.Abs(surveysChange),
                    ChangeType = surveysChange >= 0 ? "increase" : "decrease"
                },
                SurveyCompletionRate = new StatItem
                {
                    Value = completionRate,
                    Change = Math.Abs(completionRateChange),
                    ChangeType = completionRateChange >= 0 ? "increase" : "decrease"
                },
                UserGrowthData = userGrowthData,
                SurveyCompletionTrends = surveyTrendsData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard overview");
            return StatusCode(500, new { message = "An error occurred while retrieving dashboard data" });
        }
    }

    private double CalculatePercentageChange(int oldValue, int newValue)
    {
        if (oldValue == 0) return newValue > 0 ? 100 : 0;
        return Math.Round((double)(newValue - oldValue) / oldValue * 100, 1);
    }

    private async Task<List<ChartDataPoint>> GetUserGrowthData(int months)
    {
        var result = new List<ChartDataPoint>();
        var now = DateTime.UtcNow;

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var count = await _context.Users
                .Where(u => u.TenantId != null && u.CreatedAt >= monthStart && u.CreatedAt < monthEnd)
                .Where(c => c.Tenant.IsActive)
                .CountAsync();

            result.Add(new ChartDataPoint
            {
                Label = monthStart.ToString("MMM"),
                Value = count
            });
        }

        return result;
    }

    private async Task<List<ChartDataPoint>> GetSurveyCompletionTrends(int months)
    {
        var result = new List<ChartDataPoint>();
        var now = DateTime.UtcNow;

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var count = await _context.SurveySubmissions
                .Where(s => s.Status == "Completed" && s.CompletedAt >= monthStart && s.CompletedAt < monthEnd)
                .Where(c => c.Tenant.IsActive)
                .CountAsync();

            result.Add(new ChartDataPoint
            {
                Label = monthStart.ToString("MMM"),
                Value = count
            });
        }

        return result;
    }
}

// Response DTOs
public class DashboardOverviewResponse
{
    public StatItem CompaniesRegistered { get; set; } = new();
    public StatItem UsersRegistered { get; set; } = new();
    public StatItem SurveysCompleted { get; set; } = new();
    public StatItem SurveyCompletionRate { get; set; } = new();
    public List<ChartDataPoint> UserGrowthData { get; set; } = new();
    public List<ChartDataPoint> SurveyCompletionTrends { get; set; } = new();
}

public class StatItem
{
    public double Value { get; set; }
    public double Change { get; set; }
    public string ChangeType { get; set; } = "increase";
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

