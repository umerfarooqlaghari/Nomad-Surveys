using System.ComponentModel.DataAnnotations;

namespace Nomad.Api.DTOs.Request;

public class SendPasswordResetOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpAndResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class SendFormReminderRequest
{
    [Required]
    public Guid SubjectEvaluatorSurveyId { get; set; }
}

