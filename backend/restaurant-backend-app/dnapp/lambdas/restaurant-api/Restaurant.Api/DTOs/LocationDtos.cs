namespace Restaurant.Api.DTOs
{
    public record DishResponse(string Name, string Price, string Weight, string ImageUrl);

    public record LocationResponse(
        string Id,
        string Address,
        string Description,
        string TotalCapacity,
        string AverageOccupancy,
        string ImageUrl,
        string Rating
    );

    public record LocationBrief(string Id, string Address);
}
