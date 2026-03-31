namespace Restaurant.Api.Contracts.Requests;

public class UpdateFeedbackRequest
{
    public required string FeedbackId { get; set; }
    public required string Comment { get; set; }
    public required int Rating { get; set; }
    public required string FeedbackType { get; set; } // feedback type "waiter" / "kitchen"
}