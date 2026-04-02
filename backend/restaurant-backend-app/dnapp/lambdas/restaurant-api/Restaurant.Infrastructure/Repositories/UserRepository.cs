using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private const string CustomerRole = "CUSTOMER";
    private const string FirstNameIndex = "customer-firstName-index";
    private const string LastNameIndex = "customer-lastName-index";
    private const string EmailIndex = "customer-email-index";

    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _client;

    public UserRepository(IDynamoDBContext context, IAmazonDynamoDB client)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _client = client;
    }

    public async Task CreateAsync(User user, CancellationToken ct, bool isOnlyUpdate = false)
    {
        if (!isOnlyUpdate)
        {
            ApplySearchFields(user);
        }
        await _context.SaveAsync(user, ct);
    }

    public Task<User?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        return _context.LoadAsync<User?>(userId, ct);
    }

    public async Task<IReadOnlyList<User>> SearchCustomersAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<User>();

        var normalizedQuery = Normalize(query);

        var byFirstNameTask = QueryByPrefixAsync(FirstNameIndex, normalizedQuery, ct);
        var byLastNameTask = QueryByPrefixAsync(LastNameIndex, normalizedQuery, ct);
        var byEmailTask = QueryByPrefixAsync(EmailIndex, normalizedQuery, ct);

        await Task.WhenAll(byFirstNameTask, byLastNameTask, byEmailTask);

        return byFirstNameTask.Result
            .Concat(byLastNameTask.Result)
            .Concat(byEmailTask.Result)
            .GroupBy(x => x.UserId)
            .Select(x => x.First())
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToList();
    }



    public async Task<(string username, string? iamgeUrl)> GetUserDataForFeedbackCreationByIdAsync(string userId, 
        CancellationToken ct = default)
    {
        const string requiredFields = "firstName,lastName,imageUrl";
        var request = FormGetRequestForFieldsGettingById(userId, requiredFields);

        var response = await _client.GetItemAsync(request, ct);

        if (response.Item.Count == 0) return (string.Empty, null);
        var firstName = response.Item.GetValueOrDefault("firstName")?.S ?? string.Empty;
        var lastName = response.Item .GetValueOrDefault("lastName")?.S ?? string.Empty;
        var username = firstName + " " + lastName;
        if (string.IsNullOrEmpty(username))
            return (string.Empty, null);

        var imageUrl = response.Item.GetValueOrDefault("imageUrl")?.S;
        return (username, imageUrl);
    }


    public async Task UpdateUserRatingAsync(string userId, int newFeedbackRating, CancellationToken ct = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Users",
            Key = new Dictionary<string, AttributeValue>
            {
                { "userId", new AttributeValue { S = userId } }
            },
            UpdateExpression = "ADD #r :newRating, #c :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#r", "rating" },
                { "#c", "feedbacksNumber" },
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":newRating", new AttributeValue { N = newFeedbackRating.ToString() } },
                { ":inc",       new AttributeValue { N = "1" } },
            },
            ReturnValues = ReturnValue.NONE
        };

        await _client.UpdateItemAsync(request, ct);
    }

    public async Task<WaiterFeedbackData> GetWaiterFeedbackDataAsync(string waiterId, CancellationToken ct = default)
    {
        var request = new GetItemRequest
        {
            TableName = "Users",
            Key = new Dictionary<string, AttributeValue>
            {
                { "userId", new AttributeValue { S = waiterId } }
            },
            ProjectionExpression = "#fn, #ln, #img, #r, #fn2",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#fn",  "firstName" },
                { "#ln",  "lastName" },
                { "#img", "imageUrl" },
                { "#r",   "rating" },
                { "#fn2", "feedbacksNumber" }
            }
        };

        var response = await _client.GetItemAsync(request, ct);

        if (!response.IsItemSet)
            throw new KeyNotFoundException($"Waiter {waiterId} not found");

        var item = response.Item;

        return new WaiterFeedbackData
        {
            WaiterName           = $"{item["firstName"].S} {item["lastName"].S}",
            WaiterImageUrl       = item.TryGetValue("imageUrl", out var img) ? img.S : null,
            WaiterRating         = int.Parse(item["rating"].N),
            WaiterFeedbacksNumber = int.Parse(item["feedbacksNumber"].N)
        };
    }

    private Task<List<User>> QueryByPrefixAsync(string indexName, string normalizedPrefix, CancellationToken ct)
    {
        var config = new DynamoDBOperationConfig
        {
            IndexName = indexName
        };

        var search = _context.QueryAsync<User>(
            CustomerRole,
            QueryOperator.BeginsWith,
            new object[] { normalizedPrefix },
            config);

        return search.GetRemainingAsync(ct);
    }

    private static void ApplySearchFields(User user)
    {
        user.Role = user.Role.Trim().ToUpperInvariant();
        user.FirstNameNormalized = Normalize(user.FirstName);
        user.LastNameNormalized = Normalize(user.LastName);
        user.EmailNormalized = Normalize(user.Email);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private GetItemRequest FormGetRequestForFieldsGettingById(string userId, string fields)
    {
        return new GetItemRequest
        {
            TableName = "Users",
            Key = new Dictionary<string, AttributeValue>
            {
                { "userId", new AttributeValue { S = userId } }
            },
            ProjectionExpression = fields
        };  
    }
    
    public async Task UpdateEmailAsync(string userId, string newEmail, CancellationToken ct = default)
    {
        var user = await _context.LoadAsync<User>(userId, ct);
        if (user is null) return;

        user.Email = newEmail;
        user.EmailNormalized = Normalize(newEmail);
        user.UpdatedAt = DateTime.UtcNow.ToString("o");

        await _context.SaveAsync(user, ct);
    }
    
    public async Task UpdateUserNameAsync(string userId, string firstName, string lastName, CancellationToken ct)
    {
        var user = await _context.LoadAsync<User>(userId, ct);
        if (user == null) return;

        user.FirstName = firstName;
        user.LastName = lastName;
        user.UpdatedAt = DateTime.UtcNow.ToString("o");

        await _context.SaveAsync(user, ct);
    }

    public async Task UpdateAvatarUrlAsync(string userId, string url, CancellationToken ct)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Users",
            Key = new Dictionary<string, AttributeValue>
            {
                { "userId", new AttributeValue { S = userId } }
            },
            UpdateExpression = "SET imageUrl = :url, UpdatedAt = :updatedAt",
            ConditionExpression = "attribute_exists(userId)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":url", new AttributeValue { S = url } },
                { ":updatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } }
            }
        };

        await _client.UpdateItemAsync(request, ct);
    }

    public async Task<bool> IfUserExistsByEmail(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var request = new QueryRequest
        {
            TableName = "Users",
            IndexName = EmailIndex,
            Limit = 1,
            Select = Select.COUNT,
            KeyConditionExpression = "#role = :role AND #email = :email",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#role", "role" },
                { "#email", "emailNormalized" }
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":role", new AttributeValue { S = CustomerRole } },
                { ":email", new AttributeValue { S = email } }
            }
        };

        var response = await _client.QueryAsync(request, ct);
        return response.Count > 0;
    }

    public async Task CreateOtp(UserOtp otp, CancellationToken ct)
    {
        await _context.SaveAsync(otp, ct);
    }

    public async Task<UserOtp?> GetOtpByEmailAsync(string email, CancellationToken ct)
    {
        return await _context.LoadAsync<UserOtp>(email, ct);
    }
}