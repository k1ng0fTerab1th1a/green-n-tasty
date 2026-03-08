namespace Restaurant.Core.SharedModels;

public record AuthResult(string IdToken, string RefreshToken, string Username, string Role);
