using System.ComponentModel.DataAnnotations;
using Nomad.Api.Enums;

namespace Nomad.Api.Entities;

public class EmailAuditLog
{
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public AuditLogStatus Status { get; set; }

    public string? AwsMessageId { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional: Include TenantId for basic filtering if needed, but no navigation property to maintain isolation
    public Guid? TenantId { get; set; }
}
