using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey("userId")]
    public string UserId { get; set; } = string.Empty;

    [DynamoDBProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [DynamoDBProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [DynamoDBProperty("email")]
    public string Email { get; set; } = string.Empty;

    [DynamoDBProperty("role")]
    public string Role { get; set; } = string.Empty;

    [DynamoDBProperty("imageUrl")]
    public string? ImageUrl { get; set; }

    [DynamoDBProperty("firstNameNormalized")]
    public string FirstNameNormalized { get; set; } = string.Empty;

    [DynamoDBProperty("lastNameNormalized")]
    public string LastNameNormalized { get; set; } = string.Empty;

    [DynamoDBProperty("emailNormalized")]
    public string EmailNormalized { get; set; } = string.Empty;

    [DynamoDBProperty("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [DynamoDBProperty("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}