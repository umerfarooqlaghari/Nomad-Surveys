using Microsoft.EntityFrameworkCore;
using Nomad.Api.Data;
using Nomad.Api.Entities;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services.Background;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public ReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReminderBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService is starting.");
        
        // Stagger startup: Wait for 1 minute to allow the app to warm up and seed data
        // without competing for database connections in the Singapore region.
        try 
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("ReminderBackgroundService checking for pending surveys...");

            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing reminders.");
            }

            // Run every 24 hours
            // For testing/demo purposes, we'll check every hour but the logic inside filters by > 7 days
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
        
        _logger.LogInformation("ReminderBackgroundService is stopping.");
    }

    private async Task ProcessRemindersAsync(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NomadSurveysDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var frontendUrl = _configuration["FrontendUrl"] ?? "https://nomadvirtual.com";

            // 1. Identify pending assignments that need reminders
            // Criteria:
            // - Assignment is Active
            // - Created more than 7 days ago
            // - Reminder NEVER sent (LastReminderSentAt is null)
            // - NOT Completed (submission status != Completed)
            
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var pendingData = await context.SubjectEvaluatorSurveys
                .AsNoTracking()
                .Where(ses => ses.IsActive 
                             && ses.CreatedAt < sevenDaysAgo 
                             && ses.LastReminderSentAt == null
                             && !context.SurveySubmissions.Any(ss => ss.SubjectEvaluatorSurveyId == ses.Id && ss.Status == SurveySubmissionStatus.Completed))
                .Select(ses => new
                {
                    ses.Id,
                    SurveyTitle = ses.Survey.Title,
                    EvaluatorEmail = ses.SubjectEvaluator.Evaluator.Employee.Email,
                    EvaluatorName = ses.SubjectEvaluator.Evaluator.Employee.FullName,
                    EvaluatorPasswordHash = ses.SubjectEvaluator.Evaluator.PasswordHash,
                    SubjectName = ses.SubjectEvaluator.Subject.Employee.FullName,
                    TenantName = ses.Tenant.Name,
                    TenantSlug = ses.Tenant.Slug
                })
                .ToListAsync(stoppingToken);

            if (!pendingData.Any())
            {
                _logger.LogInformation("No pending assignments requiring reminders found.");
                return;
            }

            // Group by Evaluator (Employee Email) to send consolidated emails
            var dataByEvaluator = pendingData
                .GroupBy(x => x.EvaluatorEmail)
                .ToList();

            _logger.LogInformation("Found {Count} evaluators with pending forms older than 7 days.", dataByEvaluator.Count);

            var allProcessedIds = new List<Guid>();

            foreach (var group in dataByEvaluator)
            {
                var evaluatorEmail = group.Key;
                var items = group.ToList();
                var firstItem = items.First();
                
                var passwordGenerator = scope.ServiceProvider.GetRequiredService<IPasswordGenerator>();
                var generatedPassword = passwordGenerator.Generate(evaluatorEmail);
                var passwordDisplay = generatedPassword;
                
                var dashboardLink = $"{frontendUrl}/{firstItem.TenantSlug}/participant/dashboard";

                var pendingItems = items.Select(a => (
                    FormTitle: a.SurveyTitle,
                    SubjectName: a.SubjectName,
                    Link: $"{frontendUrl}/{firstItem.TenantSlug}/participant/forms/{a.Id}"
                )).ToList();

                // Send Email
                var success = await emailService.SendConsolidatedReminderEmailAsync(
                    evaluatorEmail,
                    firstItem.EvaluatorName,
                    items.Count,
                    pendingItems,
                    dashboardLink,
                    firstItem.TenantName,
                    firstItem.TenantSlug,
                    passwordDisplay
                );

                if (success)
                {
                    allProcessedIds.AddRange(items.Select(i => i.Id));
                    _logger.LogInformation("Sent reminder to {Email} for {Count} forms.", evaluatorEmail, items.Count);
                }
                else
                {
                    _logger.LogError("Failed to send reminder email to {Email}.", evaluatorEmail);
                }
            }

            if (allProcessedIds.Any())
            {
                // Efficiently update using ExecuteUpdateAsync (EF Core 9 feature)
                await context.SubjectEvaluatorSurveys
                    .Where(ses => allProcessedIds.Contains(ses.Id))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.LastReminderSentAt, DateTime.UtcNow), stoppingToken);
            }
        }
    }
}
