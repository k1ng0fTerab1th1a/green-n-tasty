namespace Restaurant.Core.Models.SharedModels
{
    public record AuthResult(string IdToken, string RefreshToken, string Username, string Role);
}