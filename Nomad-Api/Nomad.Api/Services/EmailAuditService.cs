using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Options;
using Nomad.Api.Configuration;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.Entities;
using Nomad.Api.Enums;
using Nomad.Api.Services.Interfaces;

namespace Nomad.Api.Services;

public class EmailAuditService : IEmailAuditService
{
    private readonly NomadSurveysDbContext _context;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailAuditService> _logger;

    public EmailAuditService(
        NomadSurveysDbContext context,
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailAuditService> logger)
    {
        _context = context;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task ProcessEmailLoopAsync(List<ParticipantEmailRequest> participants)
    {
        var awsSettings = _emailSettings.AWS;

        if (string.IsNullOrEmpty(awsSettings.AccessKeyId) || string.IsNullOrEmpty(awsSettings.SecretAccessKey))
        {
            _logger.LogError("AWS SES credentials are not configured in appsettings.json");
            return;
        }

        using var client = new AmazonSimpleEmailServiceClient(
            awsSettings.AccessKeyId,
            awsSettings.SecretAccessKey,
            RegionEndpoint.GetBySystemName(awsSettings.Region));

        foreach (var participant in participants)
        {
            Guid? auditLogId = null;

            // 1. Create PENDING record (Failsafe for DB)
            try
            {
                var auditLog = new EmailAuditLog
                {
                    Id = Guid.NewGuid(),
                    Email = participant.Email,
                    Status = AuditLogStatus.PENDING,
                    TenantId = participant.TenantId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.EmailAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                auditLogId = auditLog.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FAILED to create initial PENDING audit log for {Email}. Loop continuing.", participant.Email);
            }

            // 2. SES Call
            try
            {
                var sendRequest = new SendEmailRequest
                {
                    Source = $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>",
                    Destination = new Destination { ToAddresses = new List<string> { participant.Email } },
                    Message = new Message
                    {
                        Subject = new Content(participant.Subject),
                        Body = new Body { Html = new Content(participant.Body) }
                    }
                };

                var response = await client.SendEmailAsync(sendRequest);

                // 3. Update to SUCCESS
                if (auditLogId.HasValue)
                {
                    await UpdateAuditLogAsync(auditLogId.Value, AuditLogStatus.SUCCESS, response.MessageId, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SES call FAILED for {Email}", participant.Email);

                // 4. Update to FAILED
                if (auditLogId.HasValue)
                {
                    await UpdateAuditLogAsync(auditLogId.Value, AuditLogStatus.FAILED, null, ex.Message);
                }
            }

            // 5. Rate limit delay (200ms)
            await Task.Delay(200);
        }
    }

    private async Task UpdateAuditLogAsync(Guid id, AuditLogStatus status, string? messageId, string? errorMessage)
    {
        try
        {
            // Find the log entry without tracking to avoid potential issues in loop
            var auditLog = await _context.EmailAuditLogs.FindAsync(id);
            if (auditLog != null)
            {
                auditLog.Status = status;
                auditLog.AwsMessageId = messageId;
                auditLog.ErrorMessage = errorMessage;
                auditLog.SentAt = (status == AuditLogStatus.SUCCESS) ? DateTime.UtcNow : null;
                
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAILED to update audit log {Id} to {Status}. Failsafe active.", id, status);
        }
    }
}
