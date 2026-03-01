namespace Restaurant.Core.ServiceDTOs;

public class FeedbackDTO
{
    public string Id { get; set; }
    public string Rate { get; set; }
    public string Comment { get; set; }
    public string UserName { get; set; }
    public string UserAvatarUrl { get; set; }
    public string Date { get; set; }
    public string Type { get; set; }
    public string LocationId { get; set; }

    public FeedbackDTO()
    {
    }

    // Manually have to set up UserName and UserAvatarUrl
    public FeedbackDTO(Feedback feedback)
    {
        Id = feedback.Id;
        Rate = feedback.Rate.ToString();
        Comment = feedback.Comment;
        Date = feedback.Date;
        Type = feedback.Type;
        LocationId = feedback.LocationId;

    }
}