namespace Restaurant.Api.Contracts.Responses;

public class UserResponse
{
	public string UserId { get; set; } = null!;
	public string FirstName { get; set; } = null!;
	public string LastName { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string Role { get; set; } = null!;
	public string? WaiterFlag { get; set; }
	public string? ImageUrl { get; set; }
	public double  Rating { get; set; }
	public int FeedbacksCount { get; set; }
}
