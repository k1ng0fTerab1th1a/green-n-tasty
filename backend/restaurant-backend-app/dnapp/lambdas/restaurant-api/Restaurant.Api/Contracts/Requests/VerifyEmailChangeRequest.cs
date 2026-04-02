using System.ComponentModel.DataAnnotations;

namespace Restaurant.Api.Contracts.Requests;

public class VerifyEmailChangeRequest
{
    [Required(ErrorMessage = "Verification code is required.")]
    public required string Code { get; set; }

    [Required(ErrorMessage = "Access token is required.")]
    public required string AccessToken { get; set; }

    [Required(ErrorMessage = "New email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string NewEmail { get; set; }
}
