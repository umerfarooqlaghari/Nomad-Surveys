namespace Nomad.Api.DTOs.Request;

public class ParticipantEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}
