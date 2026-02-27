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

    public async Task<bool> SendFormAssignmentEmailAsync(string toEmail, string evaluatorName, string subjectName, string formTitle, string formLink, string tenantName, string tenantSlug, string passwordDisplay)
    {
        var subject = $"New Form Assigned: {formTitle}";
        var htmlBody = GenerateFormAssignmentEmailHtml(evaluatorName, subjectName, formTitle, formLink, tenantName, toEmail, tenantSlug, passwordDisplay);

        return await SendEmailAsync(toEmail, subject, htmlBody, evaluatorName);
    }

    public async Task<bool> SendFormReminderEmailAsync(string toEmail, string evaluatorName, string subjectName, string formTitle, string formLink, string dueDate, string tenantName, string tenantSlug, string passwordDisplay)
    {
        var subject = $"Reminder: Complete {formTitle}";
        var htmlBody = GenerateFormReminderEmailHtml(evaluatorName, subjectName, formTitle, formLink, dueDate, tenantName, toEmail, tenantSlug, passwordDisplay);

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
                                This code will expire in 2 minutes. If you didn't request a password reset, please ignore this email.
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

    private string GenerateFormAssignmentEmailHtml(string evaluatorName, string subjectName, string formTitle, string formLink, string tenantName, string email, string tenantSlug, string password)
    {
        var loginInfo = $@"
            <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 4px; margin: 10px 0;"">
                <strong>Company Code:</strong> {tenantSlug}<br>
                <strong>Email:</strong> {email}<br>
                <strong>Password:</strong> {password}
            </div>";

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
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Dear {evaluatorName},
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Ascend Consulting is approaching you as an independent organization to seek your participation in a 360° Feedback Survey. Ascend is a leading consulting firm based in Pakistan with expertise in multi-source feedback, organizational transformation, executive search and experiential learning.
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                The purpose of the survey is to foster culture of learning at Ascend, where you and your colleagues can exchange feedback to develop self-awareness, identify key strengths and development areas to work on, towards the journey of personal and professional growth.
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                As part of the program, you have been nominated to share feedback for your following colleagues:
                            </p>
                            
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #111827; font-weight: 600;"">
                                {subjectName}
                            </p>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Kindly provide feedback based on your relationship with them i.e., Line Manager, Direct Report, Peer or Stakeholder.
                            </p>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Next, please follow the steps below to log in and complete the survey(s):<br>
                                Please click here: <a href=""{formLink}"" style=""color: #3b82f6; text-decoration: none;"">Take the Evaluation</a>
                            </p>

                            <p style=""margin: 0 0 16px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Use the following credentials to log in:
                            </p>
                            {loginInfo}
                            
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Once you have entered your Username (Email) and Password and clicked on 'Sign In', you will be directed to your portal where you can see the surveys you have to complete. If the generated password is not working, please reset your password manually to your preference.
                            </p>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Follow the instructions thereafter to complete your surveys.
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Please note the following while you are completing surveys:
                                <ul style=""margin-top: 8px;"">
                                    <li>Each survey will take approximately 20 minutes.</li>
                                    <li>Please make sure that you have a stable internet connection while responding to surveys.</li>
                                    <li>The survey can be accessed 24/7. You can complete your surveys at intervals.</li>
                                    <li>The survey can be accessed on desktop/laptop and mobile phone.</li>
                                    <li>The survey(s) should be completed latest by 8th January 2026.</li>
                                </ul>
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                If you are facing any technical difficulty in accessing or completing the survey, please refer to our User Manual. In case you are unable to find an answer to your query, feel free to contact us at hello@ascendevelopment.com.
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Thank you for your participation.
                            </p>
                            <p style=""margin: 0; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Regards,<br>
                                Ascend Consulting
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

    private string GenerateFormReminderEmailHtml(string evaluatorName, string subjectName, string formTitle, string formLink, string dueDate, string tenantName, string email, string tenantSlug, string password)
    {
        var loginInfo = $@"
            <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 4px; margin: 10px 0;"">
                <strong>Company Code:</strong> {tenantSlug}<br>
                <strong>Email:</strong> {email}<br>
                <strong>Password:</strong> {password}
            </div>";

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
                                Dear {evaluatorName},
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Ascend Consulting is approaching you as an independent organization to seek your participation in a 360° Feedback Survey. This is to remind you that your feedback for one or more survey(s) is pending. The deadline to complete the survey was till 8th January 2026.
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                As a participant you have been nominated to provide feedback for your following colleague(s) and/or complete a self-evaluation:
                            </p>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #111827; font-weight: 600;"">
                                {subjectName}
                            </p>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Rest assured your responses would be kept strictly confidential and under no circumstances be shared with the organization or anyone.
                            </p>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                You can access the survey 24/7 on your laptop/desktop or mobile phone by clicking here: <a href=""{formLink}"" style=""color: #3b82f6; text-decoration: none;"">Take the Evaluation</a>
                            </p>

                            <p style=""margin: 0 0 16px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Please note your credentials:
                            </p>
                            {loginInfo}
                            
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                In case you have accessed the link before and changed your password, your password will be your updated password. If the generated password is not working, please reset your password manually to your preference.
                            </p>

                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                If you are facing any technical difficulty in accessing or completing the survey, please refer to our User Manual. In case you are unable to find an answer to your query, feel free to contact us at hello@ascendevelopment.com.
                            </p>
                            
                            <p style=""margin: 0; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Regards,<br>
                                Ascend Consulting
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
    public async Task<bool> SendBulkFormAssignmentEmailAsync(string toEmail, string evaluatorName, int formCount, string formTitle, string dashboardLink, string tenantName, string tenantSlug, string passwordDisplay)
    {
        var subject = $"You Have {formCount} New Forms Assigned: {formTitle}";
        var htmlBody = GenerateBulkFormAssignmentEmailHtml(evaluatorName, formCount, formTitle, dashboardLink, tenantName, toEmail, tenantSlug, passwordDisplay);

        return await SendEmailAsync(toEmail, subject, htmlBody, evaluatorName);
    }

    public async Task<bool> SendConsolidatedReminderEmailAsync(string toEmail, string evaluatorName, int pendingCount, List<(string FormTitle, string SubjectName, string Link)> pendingItems, string dashboardLink, string tenantName, string tenantSlug, string passwordDisplay)
    {
        var subject = $"Action Required: You have {pendingCount} pending evaluations";
        var htmlBody = GenerateConsolidatedReminderEmailHtml(evaluatorName, pendingCount, pendingItems, dashboardLink, tenantName, toEmail, tenantSlug, passwordDisplay);

        return await SendEmailAsync(toEmail, subject, htmlBody, evaluatorName);
    }

    private string GenerateConsolidatedReminderEmailHtml(string evaluatorName, int pendingCount, List<(string FormTitle, string SubjectName, string Link)> pendingItems, string dashboardLink, string tenantName, string email, string tenantSlug, string password)
    {
        var loginInfo = $@"
            <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 4px; margin: 10px 0;"">
                <strong>Company Code:</strong> {tenantSlug}<br>
                <strong>Email:</strong> {email}<br>
                <strong>Password:</strong> {password}
            </div>";

        var itemsHtml = "";
        foreach (var item in pendingItems.Take(5)) // Show max 5 items in email
        {
            itemsHtml += $@"
            <tr>
                <td style=""padding: 12px 16px; border-bottom: 1px solid #e5e7eb; font-size: 14px; color: #111827;"">
                    <div style=""font-weight: 500;"">{item.FormTitle}</div>
                    <div style=""color: #6b7280; font-size: 13px;"">Subject: {item.SubjectName}</div>
                </td>
                <td style=""padding: 12px 16px; border-bottom: 1px solid #e5e7eb; text-align: right;"">
                    <a href=""{item.Link}"" style=""font-size: 13px; font-weight: 500; color: #3b82f6; text-decoration: none;"">Start</a>
                </td>
            </tr>";
        }

        if (pendingItems.Count > 5)
        {
            itemsHtml += $@"
            <tr>
                <td colspan=""2"" style=""padding: 12px 16px; font-size: 13px; color: #6b7280; text-align: center;"">
                    ...and {pendingItems.Count - 5} more pending forms
                </td>
            </tr>";
        }

        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <title>Pending Evaluations Reminder</title>
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
                                    <h2 style=""margin: 0 0 16px; font-size: 20px; font-weight: 600; color: #111827;"">Pending Evaluations Reminder</h2>
                                    <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        Dear {evaluatorName},
                                    </p>
                                    <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        Ascend Consulting is approaching you as an independent organization to seek your participation in a 360° Feedback Survey. This is to remind you that your feedback for one or more survey(s) is pending. The deadline to complete the survey was till 8th January 2026.
                                    </p>
                                    
                                    <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        As a participant you have been nominated to provide feedback for your following colleague(s) and/or complete a self-evaluation.
                                    </p>
                                    
                                    <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        This is a friendly reminder that you have <strong>{pendingCount}</strong> pending evaluations that require your attention.
                                    </p>

                                    <!-- List Container -->
                                    <div style=""border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden; margin-bottom: 24px;"">
                                        <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                                            {itemsHtml}
                                        </table>
                                    </div>
                                    
                                    <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        Rest assured your responses would be kept strictly confidential and under no circumstances be shared with the organization or anyone.
                                    </p>

                                    <!-- CTA Button -->
                                    <table role=""presentation"" style=""margin: 0 0 24px;"">
                                        <tr>
                                            <td style=""border-radius: 6px; background-color: #f59e0b;"">
                                                <a href=""{dashboardLink}"" style=""display: inline-block; padding: 12px 32px; font-size: 15px; font-weight: 500; color: #ffffff; text-decoration: none;"">
                                                    Go to Dashboard
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                    
                                    <p style=""margin: 0 0 16px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        Please note your credentials:
                                    </p>
                                    {loginInfo}
                                    
                                    <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        In case you have accessed the link before and changed your password, your password will be your updated password. If the generated password is not working, please reset your password manually to your preference.
                                    </p>
                                    
                                    <p style=""margin: 0; font-size: 13px; line-height: 20px; color: #9ca3af;"">
                                        If the button doesn't work, copy and paste this link into your browser:<br>
                                        <a href=""{dashboardLink}"" style=""color: #f59e0b; text-decoration: none;"">{dashboardLink}</a>
                                    </p>
                                    
                                    <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        If you are facing any technical difficulty in accessing or completing the survey, please refer to our User Manual. In case you are unable to find an answer to your query, feel free to contact us at hello@ascendevelopment.com.
                                    </p>
                                    
                                    <p style=""margin: 0; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                        Regards,<br>
                                        Ascend Consulting
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

    private string GenerateBulkFormAssignmentEmailHtml(string evaluatorName, int formCount, string formTitle, string dashboardLink, string tenantName, string email, string tenantSlug, string password)
    {
        var loginInfo = $@"
            <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 4px; margin: 10px 0;"">
                <strong>Company Code:</strong> {tenantSlug}<br>
                <strong>Email:</strong> {email}<br>
                <strong>Password:</strong> {password}
            </div>";

        return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <title>Bulk Form Assignment</title>
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
                                <h2 style=""margin: 0 0 16px; font-size: 20px; font-weight: 600; color: #111827;"">You Have {formCount} New Forms Assigned</h2>
                                <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                    Dear {evaluatorName},
                                </p>
                                <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                    Ascend Consulting is approaching you as an independent organization to seek your participation in a 360° Feedback Survey. Ascend is a leading consulting firm based in Pakistan with expertise in multi-source feedback, organizational transformation, executive search and experiential learning.
                                </p>
                                <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                    You have been assigned <strong>{formCount}</strong> new evaluation forms titled <strong>{formTitle}</strong>.
                                </p>
                                
                                <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                    Next, please follow the steps below to log in and complete the survey(s):<br>
                                    Please click here: <a href=""{dashboardLink}"" style=""color: #3b82f6; text-decoration: none;"">Take the Evaluation</a>
                                </p>
                                
                                <p style=""margin: 0 0 16px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                    Use the following credentials to log in:
                                </p>
                                {loginInfo}
                                
                                <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                    Once you have entered your Username (Email) and Password and clicked on 'Sign In', you will be prompted to reset your password. Enter your new password and then click on the 'Reset Password' button. You can then click on 'Sign in'. thereafter, you will be directed to your portal where you can see the surveys you have to complete.
                                </p>
                                
                                <!-- CTA Button -->
                                <table role=""presentation"" style=""margin: 0 0 24px;"">
                                    <tr>
                                        <td style=""border-radius: 6px; background-color: #3b82f6;"">
                                            <a href=""{dashboardLink}"" style=""display: inline-block; padding: 12px 32px; font-size: 15px; font-weight: 500; color: #ffffff; text-decoration: none;"">
                                                Go to Dashboard
                                            </a>
                                        </td>
                                    </tr>
                                </table>
                                <p style=""margin: 0; font-size: 13px; line-height: 20px; color: #9ca3af;"">
                                    If the button doesn't work, copy and paste this link into your browser:<br>
                                    <a href=""{dashboardLink}"" style=""color: #3b82f6; text-decoration: none;"">{dashboardLink}</a>
                                </p>
                                
                                <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                    Thank you for your participation.
                                </p>
                                <p style=""margin: 0; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                    Regards,<br>
                                    Ascend Consulting
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

    public async Task<bool> SendConsolidatedAssignmentEmailAsync(string toEmail, string evaluatorName, int formCount, List<(string FormTitle, string SubjectName)> assignedItems, string dashboardLink, string tenantName, string tenantSlug, string passwordDisplay)
    {
        var subject = $"New Forms Assigned: You have {formCount} new evaluations";
        var htmlBody = GenerateConsolidatedAssignmentEmailHtml(evaluatorName, formCount, assignedItems, dashboardLink, tenantName, toEmail, tenantSlug, passwordDisplay);

        return await SendEmailAsync(toEmail, subject, htmlBody, evaluatorName);
    }

    private string GenerateConsolidatedAssignmentEmailHtml(string evaluatorName, int formCount, List<(string FormTitle, string SubjectName)> assignedItems, string dashboardLink, string tenantName, string email, string tenantSlug, string password)
    {
        var loginInfo = $@"
            <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 4px; margin: 10px 0;"">
                <strong>Company Code:</strong> {tenantSlug}<br>
                <strong>Email:</strong> {email}<br>
                <strong>Password:</strong> {password}
            </div>";

        var itemsHtml = "";
        foreach (var item in assignedItems.Take(10)) // Show max 10 items in email
        {
            itemsHtml += $@"
            <tr>
                <td style=""padding: 12px 16px; border-bottom: 1px solid #e5e7eb; font-size: 14px; color: #111827;"">
                    <div style=""font-weight: 500;"">{item.FormTitle}</div>
                    <div style=""color: #6b7280; font-size: 13px;"">Subject: {item.SubjectName}</div>
                </td>
            </tr>";
        }

        if (assignedItems.Count > 10)
        {
            itemsHtml += $@"
            <tr>
                <td style=""padding: 12px 16px; font-size: 13px; color: #6b7280; text-align: center;"">
                    ...and {assignedItems.Count - 10} more new forms
                </td>
            </tr>";
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>New Forms Assigned</title>
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
                            <h2 style=""margin: 0 0 16px; font-size: 20px; font-weight: 600; color: #111827;"">New Forms Assigned</h2>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Dear {evaluatorName},
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Ascend Consulting is approaching you as an independent organization to seek your participation in a 360° Feedback Survey. You have been assigned to provide feedback for your following colleague(s) and/or complete a self-evaluation.
                            </p>
                            
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                You have <strong>{formCount}</strong> new evaluation forms that require your attention.
                            </p>

                            <!-- List Container -->
                            <div style=""border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden; margin-bottom: 24px;"">
                                <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                                    {itemsHtml}
                                </table>
                            </div>

                            <!-- CTA Button -->
                            <table role=""presentation"" style=""margin: 0 0 24px;"">
                                <tr>
                                    <td style=""border-radius: 6px; background-color: #3b82f6;"">
                                        <a href=""{dashboardLink}"" style=""display: inline-block; padding: 12px 32px; font-size: 15px; font-weight: 500; color: #ffffff; text-decoration: none;"">
                                            Go to Dashboard
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style=""margin: 0 0 16px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Please note your credentials:
                            </p>
                            {loginInfo}
                            
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Once you have entered your Username (Email) and Password and clicked on 'Sign In', you will be directed to your portal where you can see the surveys you have to complete.
                            </p>
                            
                            <p style=""margin: 0; font-size: 15px; line-height: 24px; color: #6b7280;"">
                                Regards,<br>
                                Ascend Consulting
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

