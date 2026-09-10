using Alpha.Api.DTOs.Request;

namespace Alpha.Api.Services.Interfaces;

public interface IEmailAuditService
{
    /// <summary>
    /// Processes a list of emails in a loop with manual delay to prevent SES burst limits.
    /// Each email is audited in the database with PENDING/SUCCESS/FAILED status.
    /// </summary>
    Task ProcessEmailLoopAsync(List<ParticipantEmailRequest> participants);
}
