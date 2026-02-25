using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey("userId")]
    public string UserId { get; set; }

    [DynamoDBProperty("firstName")]
    public string FirstName { get; set; }

    [DynamoDBProperty("lastName")]
    public string LastName { get; set; }

    [DynamoDBProperty("email")]
    public string Email { get; set; }

    [DynamoDBProperty("role")]
    public string Role { get; set; }

    [DynamoDBProperty("imageUrl")]
    public string ImageUrl { get; set; }

    [DynamoDBProperty("createdAt")]
    public string CreatedAt { get; set; }

    [DynamoDBProperty("updatedAt")]
    public string UpdatedAt { get; set; }
}