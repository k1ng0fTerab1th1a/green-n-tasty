namespace Restaurant.Core.Models;

public record AuthResult(string AccessToken, string RefreshToken, string Username, string Role);