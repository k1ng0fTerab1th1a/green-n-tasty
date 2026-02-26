namespace Restaurant.Api.DTOs;

public record SignUpRequest(string FirstName, string LastName, string Email, string Password);
public record SignInRequest(string Email, string Password);