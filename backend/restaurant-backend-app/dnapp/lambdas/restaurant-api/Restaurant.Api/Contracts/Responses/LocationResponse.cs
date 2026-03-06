namespace Restaurant.Api.Contracts.Responses
{
    public record LocationResponse(
        string Id,
        string Address,
        string TimeZone,
        string OpenTime,
        string CloseTime,
        string Description,
        int TotalCapacity,
        double AverageOccupancy,
        string ImageUrl,
        double Rating
    );

    public record LocationBrief(string Id, string Address);
}
