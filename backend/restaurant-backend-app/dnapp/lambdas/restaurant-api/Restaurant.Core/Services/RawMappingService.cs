using Amazon.DynamoDBv2.Model;

namespace Restaurant.Core.Services;

public class RawMappingService : IRawMappingService
{
    public Feedback MapToFeedback(Dictionary<string, AttributeValue> item)
    {
        return new Feedback
        {
            Id = item.TryGetValue("id", out var id) ? id.S : string.Empty,
            Rate = item.TryGetValue("rate", out var rate) ? int.Parse(rate.N) : 0,
            Comment = item.TryGetValue("comment", out var comment) ? comment.S : string.Empty,
            UserId = item.TryGetValue("userId", out var userId) ? userId.S : string.Empty,
            Date = item.TryGetValue("date", out var date) ? date.S : string.Empty,
            LocationId = item.TryGetValue("locationId", out var locId) ? locId.S : string.Empty,
            Type = item.TryGetValue("type", out var type) ? type.S : string.Empty
        };
    }

    public IEnumerable<Feedback> MapToFeedback(List<Dictionary<string, AttributeValue>> items)
    {
        return items.Select(MapToFeedback);
    }
}