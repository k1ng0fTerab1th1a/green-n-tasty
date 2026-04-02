namespace Restaurant.Core.SharedModels;

public record AuthResult(string IdToken, string AccessToken, string RefreshToken, string Username, string Role);
