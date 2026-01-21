using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomad.Api.DTOs.Request;
using Nomad.Api.Services.Interfaces;
using Nomad.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Nomad.Api.Controllers;

[ApiController]
[Route("{tenantSlug}/api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly NomadSurveysDbContext _context;
    private readonly ILogger<EmailController> _logger;
    private const string DefaultPassword = "Password@123";

    public EmailController(
        IEmailService emailService,
        NomadSurveysDbContext context,
        ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Send password reset OTP email to participant
    /// </summary>
    [HttpPost("send-password-reset-otp")]
    [AllowAnonymous]
    public async Task<ActionResult> SendPasswordResetOtp(
        [FromRoute] string tenantSlug,
        [FromBody] SendPasswordResetOtpRequest request)
    {
        try
        {
            // Get tenant
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive);

            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id);

            if (user == null)
            {
                // Don't reveal if user exists or not for security
                return Ok(new { message = "If the email exists, a password reset code has been sent" });
            }

            // Generate OTP (6-digit code)
            var otp = new Random().Next(100000, 999999).ToString();

            // Store OTP in user's SecurityStamp temporarily (in production, use a dedicated OTP table)
            // Format: otp|expiryUnix|attempts
            var expiryUnix = DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds();
            user.SecurityStamp = $"{otp}|{expiryUnix}|0";
            await _context.SaveChangesAsync();

            // Send email
            var success = await _emailService.SendPasswordResetOtpEmailAsync(
                user.Email!,
                $"{user.FirstName} {user.LastName}",
                otp,
                tenant.Name
            );

            if (!success)
            {
                _logger.LogWarning("Failed to send password reset email to {Email}", user.Email);
            }

            return Ok(new { message = "If the email exists, a password reset code has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset OTP");
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Verify OTP and reset password
    /// </summary>
    [HttpPost("verify-otp-and-reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> VerifyOtpAndResetPassword(
        [FromRoute] string tenantSlug,
        [FromBody] VerifyOtpAndResetPasswordRequest request)
    {
        try
        {
            // Get tenant
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive);

            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id);

            if (user == null || string.IsNullOrEmpty(user.SecurityStamp))
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            // Parse stored OTP, expiry, and attempts
            var parts = user.SecurityStamp.Split('|');
            if (parts.Length < 2)
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            var storedOtp = parts[0];
            if (!long.TryParse(parts[1], out var expiryUnix))
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            var attempts = 0;
            if (parts.Length >= 3 && int.TryParse(parts[2], out var parsedAttempts))
            {
                attempts = parsedAttempts;
            }

            // Check if max attempts (5) reached
            if (attempts >= 5)
            {
                return BadRequest(new { message = "Too many failed attempts. Please request a new code." });
            }

            // Verify OTP and expiry
            var currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (storedOtp != request.Otp || currentUnix > expiryUnix)
            {
                // Increment attempts on failure
                attempts++;
                user.SecurityStamp = $"{storedOtp}|{expiryUnix}|{attempts}";
                await _context.SaveChangesAsync();

                var remaining = 5 - attempts;
                var message = remaining > 0 
                    ? $"Invalid or expired OTP. {remaining} attempts remaining." 
                    : "Too many failed attempts. Please request a new code.";
                
                return BadRequest(new { message });
            }

            // Reset password using UserManager
            var userManager = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Nomad.Api.Entities.ApplicationUser>>();
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to reset password", errors = result.Errors });
            }

            // Clear OTP
            user.SecurityStamp = Guid.NewGuid().ToString();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP and resetting password");
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Send reminder email for pending form
    /// </summary>
    [HttpPost("send-form-reminder")]
    [Authorize]
    public async Task<ActionResult> SendFormReminder(
        [FromRoute] string tenantSlug,
        [FromBody] SendFormReminderRequest request)
    {
        try
        {
            // Get tenant
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive);

            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            // Get the survey assignment with all related data
            var assignment = await _context.SubjectEvaluatorSurveys
                .Include(ses => ses.Survey)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Subject)
                        .ThenInclude(s => s.Employee)
                .Include(ses => ses.SubjectEvaluator)
                    .ThenInclude(se => se.Evaluator)
                        .ThenInclude(e => e.Employee)
                .FirstOrDefaultAsync(ses => ses.Id == request.SubjectEvaluatorSurveyId &&
                                           ses.TenantId == tenant.Id &&
                                           ses.IsActive);

            if (assignment == null)
            {
                return NotFound(new { message = "Form assignment not found" });
            }

            // Check if form is already submitted
            var submission = await _context.SurveySubmissions
                .FirstOrDefaultAsync(ss => ss.SubjectEvaluatorSurveyId == assignment.Id);

            if (submission != null && submission.Status == "Completed")
            {
                return BadRequest(new { message = "Form has already been completed" });
            }

            // Get frontend URL from configuration
            var frontendUrl = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["FrontendUrl"];
            var formLink = $"{frontendUrl}";
            // var formLink = $"{frontendUrl}/{tenantSlug}/participant/forms/{assignment.Id}";


            // Determine due date (if available)
            var dueDate =  "No specific deadline";

            var isDefaultPassword = BCrypt.Net.BCrypt.Verify(DefaultPassword, assignment.SubjectEvaluator.Evaluator.PasswordHash);
            var passwordDisplay = isDefaultPassword ? DefaultPassword : "omitted for privacy";

            // Send reminder email
            var success = await _emailService.SendFormReminderEmailAsync(
                assignment.SubjectEvaluator.Evaluator.Employee.Email,
                assignment.SubjectEvaluator.Evaluator.Employee.FullName,
                assignment.SubjectEvaluator.Subject.Employee.FullName,
                assignment.Survey.Title,
                formLink,
                dueDate,
                tenant.Name,
                tenantSlug,
                passwordDisplay
            );

            if (!success)
            {
                _logger.LogWarning("Failed to send reminder email for assignment {AssignmentId}", assignment.Id);
                return StatusCode(500, new { message = "Failed to send reminder email" });
            }

            return Ok(new { message = "Reminder email sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending form reminder email");
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    [HttpPost("trigger-reminders")]
    [AllowAnonymous] // For testing purposes only
    public async Task<ActionResult> TriggerReminders()
    {
        try
        {
            var backgroundService = HttpContext.RequestServices.GetServices<IHostedService>()
                .OfType<Nomad.Api.Services.Background.ReminderBackgroundService>()
                .FirstOrDefault();

            if (backgroundService == null)
            {
                return NotFound(new { message = "Background service not found" });
            }

            // We can't directly call the protected/private methods easily without reflection or exposing them.
            // For a quick verification, we'll use reflection to invoke 'ProcessRemindersAsync'.
            
            var methodInfo = typeof(Nomad.Api.Services.Background.ReminderBackgroundService)
                .GetMethod("ProcessRemindersAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (methodInfo == null)
            {
                 return StatusCode(500, new { message = "Method not found" });
            }

            var task = (Task)methodInfo.Invoke(backgroundService, new object[] { CancellationToken.None })!;
            await task;

            return Ok(new { message = "Reminders triggered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering reminders");
            return StatusCode(500, new { message = $"Error: {ex.Message}" });
        }
    }
}

