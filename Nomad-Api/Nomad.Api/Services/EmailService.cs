using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Options;
using Nomad.Api.Configuration;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IAmazonSimpleEmailService _sesClient;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;

        // Initialize AWS SES client
        var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(
            _emailSettings.AWS.AccessKeyId,
            _emailSettings.AWS.SecretAccessKey
        );

        _sesClient = new AmazonSimpleEmailServiceClient(
            awsCredentials,
            RegionEndpoint.GetBySystemName(_emailSettings.AWS.Region)
        );
    }

    public async Task<bool> SendPasswordResetOtpEmailAsync(string toEmail, string toName, string otp, string tenantName)
    {
        var subject = "Reset Your Password - Nomad Surveys";
        var htmlBody = GeneratePasswordResetOtpEmailHtml(toName, otp, tenantName);

        return await SendEmailAsync(toEmail, subject, htmlBody, toName);
    }

    public async Task<bool> SendFormAssignmentEmailAsync(string toEmail, string evaluatorName, string subjectName, string formTitle, string formLink, string tenantName)
    {
        var subject = $"New Form Assigned: {formTitle}";
        var htmlBody = GenerateFormAssignmentEmailHtml(evaluatorName, subjectName, formTitle, formLink, tenantName);

        return await SendEmailAsync(toEmail, subject, htmlBody, evaluatorName);
    }

    public async Task<bool> SendFormReminderEmailAsync(string toEmail, string evaluatorName, string subjectName, string formTitle, string formLink, string dueDate, string tenantName)
    {
        var subject = $"Reminder: Complete {formTitle}";
        var htmlBody = GenerateFormReminderEmailHtml(evaluatorName, subjectName, formTitle, formLink, dueDate, tenantName);

        return await SendEmailAsync(toEmail, subject, htmlBody, evaluatorName);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? toName = null)
    {
        try
        {
            var sendRequest = new SendEmailRequest
            {
                Source = $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>",
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        }
                    }
                }
            };

            var response = await _sesClient.SendEmailAsync(sendRequest);

            _logger.LogInformation("Email sent successfully to {Email}. MessageId: {MessageId}", toEmail, response.MessageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    private string GeneratePasswordResetOtpEmailHtml(string toName, string otp, string tenantName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset Your Password</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; max-width: 100%; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""padding: 40px 40px 30px; text-align: center; border-bottom: 1px solid #e5e7eb;"">
                            <h1 style=""margin: 0; font-size: 24px; font-weight: 600; color: #111827;"">{tenantName}</h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <h2 style=""margin: 0 0 16px; font-size: 20px; font-weight: 600; color: #111827;"">Reset Your Password</h2>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Hello {toName},
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                We received a request to reset your password. Use the verification code below to complete the process:
                            </p>
                            
                            <!-- OTP Box -->
                            <div style=""background-color: #f9fafb; border: 2px solid #e5e7eb; border-radius: 8px; padding: 24px; text-align: center; margin: 0 0 24px;"">
                                <div style=""font-size: 32px; font-weight: 700; letter-spacing: 8px; color: #111827; font-family: 'Courier New', monospace;"">{otp}</div>
                            </div>
                            
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                This code will expire in 10 minutes. If you didn't request a password reset, please ignore this email.
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 30px 40px; background-color: #f9fafb; border-top: 1px solid #e5e7eb; border-radius: 0 0 8px 8px;"">
                            <p style=""margin: 0; font-size: 13px; line-height: 20px; color: #9ca3af; text-align: center;"">
                                This is an automated message from Nomad Surveys. Please do not reply to this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GenerateFormAssignmentEmailHtml(string evaluatorName, string subjectName, string formTitle, string formLink, string tenantName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>New Form Assigned</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; max-width: 100%; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""padding: 40px 40px 30px; text-align: center; border-bottom: 1px solid #e5e7eb;"">
                            <h1 style=""margin: 0; font-size: 24px; font-weight: 600; color: #111827;"">{tenantName}</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <h2 style=""margin: 0 0 16px; font-size: 20px; font-weight: 600; color: #111827;"">New Form Assigned</h2>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Hello {evaluatorName},
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                You have been assigned a new evaluation form to complete.
                            </p>

                            <!-- Form Details Box -->
                            <div style=""background-color: #f9fafb; border-left: 4px solid #3b82f6; border-radius: 4px; padding: 20px; margin: 0 0 24px;"">
                                <table role=""presentation"" style=""width: 100%;"">
                                    <tr>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #6b7280; width: 120px;"">Form Title:</td>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #111827; font-weight: 500;"">{formTitle}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #6b7280;"">Subject:</td>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #111827; font-weight: 500;"">{subjectName}</td>
                                    </tr>
                                </table>
                            </div>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Please complete this evaluation at your earliest convenience.
                            </p>

                            <!-- CTA Button -->
                            <table role=""presentation"" style=""margin: 0 0 24px;"">
                                <tr>
                                    <td style=""border-radius: 6px; background-color: #3b82f6;"">
                                        <a href=""{formLink}"" style=""display: inline-block; padding: 12px 32px; font-size: 15px; font-weight: 500; color: #ffffff; text-decoration: none;"">
                                            Complete Form
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0; font-size: 13px; line-height: 20px; color: #9ca3af;"">
                                If the button doesn't work, copy and paste this link into your browser:<br>
                                <a href=""{formLink}"" style=""color: #3b82f6; text-decoration: none;"">{formLink}</a>
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 30px 40px; background-color: #f9fafb; border-top: 1px solid #e5e7eb; border-radius: 0 0 8px 8px;"">
                            <p style=""margin: 0; font-size: 13px; line-height: 20px; color: #9ca3af; text-align: center;"">
                                This is an automated message from Nomad Surveys. Please do not reply to this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GenerateFormReminderEmailHtml(string evaluatorName, string subjectName, string formTitle, string formLink, string dueDate, string tenantName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Form Reminder</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; max-width: 100%; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""padding: 40px 40px 30px; text-align: center; border-bottom: 1px solid #e5e7eb;"">
                            <h1 style=""margin: 0; font-size: 24px; font-weight: 600; color: #111827;"">{tenantName}</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <h2 style=""margin: 0 0 16px; font-size: 20px; font-weight: 600; color: #111827;"">Reminder: Pending Form</h2>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Hello {evaluatorName},
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                This is a friendly reminder that you have a pending evaluation form that needs to be completed.
                            </p>

                            <!-- Form Details Box -->
                            <div style=""background-color: #fef3c7; border-left: 4px solid #f59e0b; border-radius: 4px; padding: 20px; margin: 0 0 24px;"">
                                <table role=""presentation"" style=""width: 100%;"">
                                    <tr>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #92400e; width: 120px;"">Form Title:</td>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #78350f; font-weight: 500;"">{formTitle}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #92400e;"">Subject:</td>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #78350f; font-weight: 500;"">{subjectName}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #92400e;"">Due Date:</td>
                                        <td style=""padding: 8px 0; font-size: 14px; color: #78350f; font-weight: 500;"">{dueDate}</td>
                                    </tr>
                                </table>
                            </div>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Please take a moment to complete this evaluation. Your feedback is valuable and appreciated.
                            </p>

                            <!-- CTA Button -->
                            <table role=""presentation"" style=""margin: 0 0 24px;"">
                                <tr>
                                    <td style=""border-radius: 6px; background-color: #f59e0b;"">
                                        <a href=""{formLink}"" style=""display: inline-block; padding: 12px 32px; font-size: 15px; font-weight: 500; color: #ffffff; text-decoration: none;"">
                                            Complete Form Now
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0; font-size: 13px; line-height: 20px; color: #9ca3af;"">
                                If the button doesn't work, copy and paste this link into your browser:<br>
                                <a href=""{formLink}"" style=""color: #f59e0b; text-decoration: none;"">{formLink}</a>
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 30px 40px; background-color: #f9fafb; border-top: 1px solid #e5e7eb; border-radius: 0 0 8px 8px;"">
                            <p style=""margin: 0; font-size: 13px; line-height: 20px; color: #9ca3af; text-align: center;"">
                                This is an automated message from Nomad Surveys. Please do not reply to this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}

