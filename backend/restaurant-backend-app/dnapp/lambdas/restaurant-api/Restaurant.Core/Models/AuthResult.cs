namespace Restaurant.Core.Models;

public record AuthResult(string IdToken, string RefreshToken, string Username, string Role);