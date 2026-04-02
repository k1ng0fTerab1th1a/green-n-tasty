using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("UserOtps")]
public class UserOtp
{
    [DynamoDBHashKey("email")]
    public string Email { get; set; } = string.Empty;
    [DynamoDBProperty("used")]
    public bool Used { get; set; }
    [DynamoDBProperty("otp")]
    public string Otp { get; set; } = string.Empty;
    [DynamoDBProperty("expiresAt")]
    public long ExpiresAt { get; set; }
    [DynamoDBProperty("created-at")] 
    public string CreatedAt { get; set; } = string.Empty; // ISO string
}