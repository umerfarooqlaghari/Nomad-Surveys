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

            // Get total counts (Value display)
            var totalCompanies = await _context.Companies
                .Include(c => c.Tenant)
                .Where(c => c.Tenant != null && c.Tenant.IsActive)
                .CountAsync();

            var totalUsers = await _context.Users
                .Include(u => u.Tenant)
                .Where(u => u.TenantId != null && u.Tenant != null && u.Tenant.IsActive)
                .CountAsync();

            var completedSurveys = await _context.SurveySubmissions
                .Include(s => s.Tenant)
                .Where(s => s.Status == "Completed" && s.Tenant != null && s.Tenant.IsActive)
                .CountAsync();

            // Get stats for current month and previous month for growth comparison
            var companiesThisMonth = await _context.Companies
                .Include(c => c.Tenant)
                .Where(c => c.CreatedAt >= startOfCurrentMonth && c.Tenant != null && c.Tenant.IsActive)
                .CountAsync();
            var companiesPrevMonth = await _context.Companies
                .Include(c => c.Tenant)
                .Where(c => c.CreatedAt >= startOfPreviousMonth && c.CreatedAt < startOfCurrentMonth && c.Tenant != null && c.Tenant.IsActive)
                .CountAsync();

            var usersThisMonth = await _context.Users
                .Include(u => u.Tenant)
                .Where(u => u.TenantId != null && u.CreatedAt >= startOfCurrentMonth && u.Tenant != null && u.Tenant.IsActive)
                .CountAsync();
            var usersPrevMonth = await _context.Users
                .Include(u => u.Tenant)
                .Where(u => u.TenantId != null && u.CreatedAt >= startOfPreviousMonth && u.CreatedAt < startOfCurrentMonth && u.Tenant != null && u.Tenant.IsActive)
                .CountAsync();

            // Surveys completed vs assigned (for the trend)
            var totalAssigned = await _context.SurveySubmissions
                .Include(s => s.Tenant)
                .Where(c => c.Tenant != null && c.Tenant.IsActive)
                .CountAsync();
            var completionRate = totalAssigned > 0 
                ? Math.Round((double)completedSurveys / totalAssigned * 100, 1)
                : 0;

            // Completion rate last month
            var totalAssignedLastMonth = await _context.SurveySubmissions
                .Include(s => s.Tenant)
                .Where(s => s.CreatedAt < startOfCurrentMonth && s.Tenant != null && s.Tenant.IsActive)
                .CountAsync();
            var completedLastMonth = await _context.SurveySubmissions
                .Include(s => s.Tenant)
                .Where(s => s.Status == "Completed" && s.CreatedAt < startOfCurrentMonth && s.Tenant != null && s.Tenant.IsActive)
                .CountAsync();
            var completionRateLastMonth = totalAssignedLastMonth > 0
                ? Math.Round((double)completedLastMonth / totalAssignedLastMonth * 100, 1)
                : 0;

            // Calculate changes
            var companiesGrowth = CalculatePercentageChange(companiesPrevMonth, companiesThisMonth);
            var usersGrowth = CalculatePercentageChange(usersPrevMonth, usersThisMonth);
            var completionRateDelta = Math.Round(completionRate - completionRateLastMonth, 1);

            // Get user growth data (last 6 months)
            var userGrowthData = await GetUserGrowthData(6);
            
            // Get survey completion trends (last 6 months)
            var surveyTrendsData = await GetSurveyCompletionTrends(6);

            return Ok(new DashboardOverviewResponse
            {
                CompaniesRegistered = new StatItem
                {
                    Value = totalCompanies,
                    Change = Math.Abs(companiesGrowth),
                    ChangeType = companiesGrowth >= 0 ? "increase" : "decrease",
                    Label = "vs last month"
                },
                UsersRegistered = new StatItem
                {
                    Value = totalUsers,
                    Change = Math.Abs(usersGrowth),
                    ChangeType = usersGrowth >= 0 ? "increase" : "decrease",
                    Label = "vs last month"
                },
                SurveysCompleted = new StatItem
                {
                    Value = completedSurveys,
                    Change = completionRate,
                    ChangeType = "increase", // Always positive context for completion rate
                    Label = "completion rate"
                },
                SurveyCompletionRate = new StatItem
                {
                    Value = completionRate,
                    Change = Math.Abs(completionRateDelta),
                    ChangeType = completionRateDelta >= 0 ? "increase" : "decrease",
                    Label = "vs last month"
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
                .Include(u => u.Tenant)
                .Where(u => u.TenantId != null && u.CreatedAt >= monthStart && u.CreatedAt < monthEnd)
                .Where(c => c.Tenant != null && c.Tenant.IsActive)
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

            var completedCount = await _context.SurveySubmissions
                .Include(s => s.Tenant)
                .Where(s => s.Status == "Completed" && s.CompletedAt >= monthStart && s.CompletedAt < monthEnd)
                .Where(c => c.Tenant != null && c.Tenant.IsActive)
                .CountAsync();

            var totalCount = await _context.SurveySubmissions
                .Include(s => s.Tenant)
                .Where(s => s.CreatedAt >= monthStart && s.CreatedAt < monthEnd)
                .Where(c => c.Tenant != null && c.Tenant.IsActive)
                .CountAsync();

            var rate = totalCount > 0 ? Math.Round((double)completedCount / totalCount * 100, 1) : 0;

            result.Add(new ChartDataPoint
            {
                Label = monthStart.ToString("MMM"),
                Value = rate
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
    public string? Label { get; set; }
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

