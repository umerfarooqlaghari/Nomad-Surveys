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
            var frontendUrl = _configuration["FrontendUrl"] ?? "https://nomad-surveys.vercel.app/";

            // 1. Identify pending assignments that need reminders
            // Criteria:
            // - Assignment is Active
            // - Created more than 7 days ago
            // - Reminder NEVER sent (LastReminderSentAt is null)
            // - NOT Completed (submission is null OR submission status != Completed)
            
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var pendingAssignments = await context.SubjectEvaluatorSurveys
                .Include(ses => ses.Survey)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Include(ses => ses.Tenant)
                .Where(ses => ses.IsActive 
                             && ses.CreatedAt < sevenDaysAgo 
                             && ses.LastReminderSentAt == null)
                .ToListAsync(stoppingToken);

            if (!pendingAssignments.Any())
            {
                _logger.LogInformation("No pending assignments requiring reminders found.");
                return;
            }

            // Filter out those that are strictly completed
            // We need to check the submissions table.
            // Since we can't easily join on the submission status in the first query without a complex join,
            // we'll fetch the IDs of completed submissions.
            
            var assignmentIds = pendingAssignments.Select(x => x.Id).ToList();
            
            var completedSubmissionIds = await context.SurveySubmissions
                .Where(ss => assignmentIds.Contains(ss.SubjectEvaluatorSurveyId) && ss.Status == SurveySubmissionStatus.Completed)
                .Select(ss => ss.SubjectEvaluatorSurveyId)
                .ToListAsync(stoppingToken);

            var reallyPendingAssignments = pendingAssignments
                .Where(x => !completedSubmissionIds.Contains(x.Id))
                .ToList();

            if (!reallyPendingAssignments.Any())
            {
                _logger.LogInformation("No incomplete assignments older than 7 days found.");
                return;
            }

            // Group by Evaluator (Employee Email) to send consolidated emails
            var assignmentsByEvaluator = reallyPendingAssignments
                .GroupBy(x => x.SubjectEvaluator.Evaluator.Employee.Email)
                .ToList();

            _logger.LogInformation("Found {Count} evaluators with pending forms older than 7 days.", assignmentsByEvaluator.Count);

            foreach (var group in assignmentsByEvaluator)
            {
                var evaluatorEmail = group.Key;
                var assignments = group.ToList();
                var evaluatorName = assignments.First().SubjectEvaluator.Evaluator.Employee.FullName;
                var tenantName = assignments.First().Tenant.Name; // Assuming same tenant for batch
                var tenantSlug = assignments.First().Tenant.Slug;
                var passwordHash = assignments.First().SubjectEvaluator.Evaluator.PasswordHash;

                var passwordGenerator = scope.ServiceProvider.GetRequiredService<IPasswordGenerator>();
                var generatedPassword = passwordGenerator.Generate(evaluatorEmail);
                var isDefaultPassword = BCrypt.Net.BCrypt.Verify(generatedPassword, passwordHash);
                var passwordDisplay = isDefaultPassword ? generatedPassword : "omitted for privacy";
                
                var dashboardLink = $"{frontendUrl}/{tenantSlug}/participant/dashboard";

                var pendingItems = assignments.Select(a => (
                    FormTitle: a.Survey.Title,
                    SubjectName: a.SubjectEvaluator.Subject.Employee.FullName,
                    Link: $"{frontendUrl}/{tenantSlug}/participant/forms/{a.Id}"
                )).ToList();

                // Send Email
                var success = await emailService.SendConsolidatedReminderEmailAsync(
                    evaluatorEmail,
                    evaluatorName,
                    assignments.Count,
                    pendingItems,
                    dashboardLink,
                    tenantName,
                    tenantSlug,
                    passwordDisplay
                );

                if (success)
                {
                    // Update LastReminderSentAt
                    foreach (var assignment in assignments)
                    {
                        assignment.LastReminderSentAt = DateTime.UtcNow;
                    }
                    _logger.LogInformation("Sent reminder to {Email} for {Count} forms.", evaluatorEmail, assignments.Count);
                }
                else
                {
                    _logger.LogError("Failed to send reminder email to {Email}.", evaluatorEmail);
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
