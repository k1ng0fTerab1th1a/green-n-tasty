using Amazon.DynamoDBv2.DataModel;

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

[DynamoDBTable("Reservations")]
public class Reservation
{
    [DynamoDBHashKey("reservationId")]
    public string ReservationId { get; set; }

    [DynamoDBProperty("tableId")]
    public string TableId { get; set; }

    [DynamoDBProperty("userId")]
    public string UserId { get; set; }

    [DynamoDBProperty("date")]
    public string Date { get; set; }

    [DynamoDBProperty("timeStart")]
    public string TimeStart { get; set; }

    [DynamoDBProperty("timeFinish")]
    public string TimeFinish { get; set; }

    [DynamoDBProperty("guestsNumber")]
    public int GuestsNumber { get; set; }

    [DynamoDBProperty("status")]
    public string Status { get; set; }

    [DynamoDBProperty("createdAt")]
    public string CreatedAt { get; set; }

    [DynamoDBProperty("updatedAt")]
    public string UpdatedAt { get; set; }

    [DynamoDBProperty("feedbackId")]
    public string? FeedbackId { get; set; }

    [DynamoDBProperty("waiterId")]
    public string? WaiterId { get; set; }
}