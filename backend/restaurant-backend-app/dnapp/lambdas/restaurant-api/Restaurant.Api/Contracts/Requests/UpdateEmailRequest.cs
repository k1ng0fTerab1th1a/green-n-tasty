using System.ComponentModel.DataAnnotations;

namespace Restaurant.Api.Contracts.Requests;

public class UpdateEmailRequest
{
    [Required(ErrorMessage = "New email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string NewEmail { get; set; }
}
