using Nomad.Api.DTOs.Request;

namespace Nomad.Api.Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Send a password reset OTP email to a participant
    /// </summary>
    Task<bool> SendPasswordResetOtpEmailAsync(string toEmail, string toName, string otp, string tenantName);

    /// <summary>
    /// Send a form assignment notification email to an evaluator
    /// </summary>
    Task<bool> SendFormAssignmentEmailAsync(string toEmail, string evaluatorName, string subjectName, string formTitle, string formLink, string tenantName);

    /// <summary>
    /// Send a reminder email for a pending/in-progress form
    /// </summary>
    Task<bool> SendFormReminderEmailAsync(string toEmail, string evaluatorName, string subjectName, string formTitle, string formLink, string dueDate, string tenantName);

    /// <summary>
    /// Send a bulk form assignment notification email to an evaluator
    /// </summary>
    Task<bool> SendBulkFormAssignmentEmailAsync(string toEmail, string evaluatorName, int formCount, string formTitle, string dashboardLink, string tenantName);

    /// <summary>
    /// Send a generic email with HTML content
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? toName = null);
}

