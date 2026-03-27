using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
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

    public async Task CreateAsync(User user)
    {
        ApplySearchFields(user);
        await _context.SaveAsync(user);
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

    public async Task<(double rating, int feedbacksAmount)> GetUserFeedbackRatingDataByIdAsync(string userId,
        CancellationToken ct = default)
    {
        const string requiredFields = "rating,feedbacksNumber";
        var request = FormGetRequestForFieldsGettingById(userId, requiredFields);

        var response = await _client.GetItemAsync(request, ct);
        
        if (response.Item.Count == 0 || 
            !response.Item.ContainsKey("rating") || 
            !response.Item.ContainsKey("feedbacksNumber"))
        {
            return (-1f, -1);
        }
        
        var rating = ParseDoubleFromAttributeValue(response.Item["rating"]);
        var feedbacksAmount = ParseIntFromAttributeValue(response.Item["feedbacksNumber"]);
        
        rating = Math.Round(rating, 2, MidpointRounding.AwayFromZero);

        return (rating, feedbacksAmount);
    }

    public async Task UpdateUserRatingAsync(string userId, double newFeedbackRating, CancellationToken ct = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Users",
            Key = new Dictionary<string, AttributeValue>
            {
                { "userId", new AttributeValue { S = userId } }
            },
            UpdateExpression = "ADD #s :newRating, #c :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#r", "rating" },
                { "#c", "feedbacksNumber" },
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":newRating", new AttributeValue { N = newFeedbackRating.ToString("F2") } },
                { ":inc", new AttributeValue { N = "1" } },
                { ":zero", new AttributeValue { N = "0" } }
            },
            ReturnValues = ReturnValue.NONE // can be set to ALL_NEW if we really need to send it to the client
        };

        await _client.UpdateItemAsync(request, ct);
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
    
    private static double ParseDoubleFromAttributeValue(AttributeValue attr)
    {
        if (string.IsNullOrEmpty(attr.N)) return -1f;
        return double.TryParse(attr.N, out var result) ? result : -1f;
    }
    
    private static int ParseIntFromAttributeValue(AttributeValue attr)
    {
        if (string.IsNullOrEmpty(attr.N)) return -1;
        return int.TryParse(attr.N, out var result) ? result : -1;
    }
}