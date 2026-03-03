namespace Restaurant.Api.Contracts.Responses
{
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
