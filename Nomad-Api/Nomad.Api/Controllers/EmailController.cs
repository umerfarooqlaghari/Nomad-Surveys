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
            user.SecurityStamp = $"{otp}:{DateTime.UtcNow.AddMinutes(10):O}";
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

            // Parse stored OTP and expiry
            var parts = user.SecurityStamp.Split(':');
            if (parts.Length != 2)
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            var storedOtp = parts[0];
            if (!DateTime.TryParse(parts[1], null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiry))
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            // Verify OTP and expiry
            if (storedOtp != request.Otp || DateTime.UtcNow > expiry)
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
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
                .GetRequiredService<IConfiguration>()["FrontendUrl"] ?? "http://localhost:3000";
            var formLink = $"{frontendUrl}/{tenantSlug}/participant/forms/{assignment.Id}";

            // Determine due date (if available)
            var dueDate =  "No specific deadline";

            // Send reminder email
            var success = await _emailService.SendFormReminderEmailAsync(
                assignment.SubjectEvaluator.Evaluator.Employee.Email,
                assignment.SubjectEvaluator.Evaluator.Employee.FullName,
                assignment.SubjectEvaluator.Subject.Employee.FullName,
                assignment.Survey.Title,
                formLink,
                dueDate,
                tenant.Name
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
}

