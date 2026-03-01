using Amazon.DynamoDBv2.Model;

namespace Restaurant.Core.Services;

public interface IRawMappingService
{
    Feedback MapToFeedback(Dictionary<string, AttributeValue> item);
    IEnumerable<Feedback> MapToFeedback(List<Dictionary<string, AttributeValue>> items);
}