namespace Restaurant.Core.DTOs;

public sealed class DishSearchResultDTO
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DishType { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
