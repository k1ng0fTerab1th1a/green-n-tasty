using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Models;

namespace Restaurant.Api.HostedServices;

public sealed class UsersBackfillHostedService : BackgroundService
{
    private static readonly string[] RequiredIndexes =
    [
        "customer-firstName-index",
        "customer-lastName-index",
        "customer-email-index"
    ];

    private static int _hasRun;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UsersBackfillHostedService> _logger;

    public UsersBackfillHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<UsersBackfillHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Interlocked.Exchange(ref _hasRun, 1) == 1)
        {
            _logger.LogInformation("Users backfill already started in this execution environment. Skipping.");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var dynamoClient = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
            var dynamoContext = scope.ServiceProvider.GetRequiredService<IDynamoDBContext>();

            await WaitForIndexesAsync(dynamoClient, stoppingToken);

            _logger.LogInformation("Starting Users backfill...");

            var scan = dynamoContext.ScanAsync<User>(new List<ScanCondition>());

            var total = 0;
            var updated = 0;
            var skipped = 0;

            do
            {
                var batch = await scan.GetNextSetAsync(stoppingToken);

                foreach (var user in batch)
                {
                    total++;

                    var normalizedRole = NormalizeRole(user.Role);
                    var firstNameNormalized = Normalize(user.FirstName);
                    var lastNameNormalized = Normalize(user.LastName);
                    var emailNormalized = Normalize(user.Email);

                    var needsUpdate =
                        user.Role != normalizedRole ||
                        user.FirstNameNormalized != firstNameNormalized ||
                        user.LastNameNormalized != lastNameNormalized ||
                        user.EmailNormalized != emailNormalized;

                    if (!needsUpdate)
                    {
                        skipped++;
                        continue;
                    }

                    user.Role = normalizedRole;
                    user.FirstNameNormalized = firstNameNormalized;
                    user.LastNameNormalized = lastNameNormalized;
                    user.EmailNormalized = emailNormalized;
                    user.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

                    await dynamoContext.SaveAsync(user, stoppingToken);
                    updated++;

                    _logger.LogInformation("Backfilled user {UserId}", user.UserId);
                }
            }
            while (!scan.IsDone);

            _logger.LogInformation(
                "Users backfill completed. Total={Total}, Updated={Updated}, Skipped={Skipped}",
                total,
                updated,
                skipped);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Users backfill was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Users backfill failed.");
            throw;
        }
    }

    private async Task WaitForIndexesAsync(IAmazonDynamoDB dynamoClient, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var table = await dynamoClient.DescribeTableAsync("Users", cancellationToken);

            var indexes = table.Table.GlobalSecondaryIndexes ?? new List<Amazon.DynamoDBv2.Model.GlobalSecondaryIndexDescription>();

            var allReady = RequiredIndexes.All(indexName =>
                indexes.Any(x =>
                    string.Equals(x.IndexName, indexName, StringComparison.Ordinal) &&
                    string.Equals(x.IndexStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase)));

            if (allReady)
            {
                _logger.LogInformation("All required Users GSIs are ACTIVE.");
                return;
            }

            _logger.LogInformation("Waiting for Users GSIs to become ACTIVE...");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizeRole(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}