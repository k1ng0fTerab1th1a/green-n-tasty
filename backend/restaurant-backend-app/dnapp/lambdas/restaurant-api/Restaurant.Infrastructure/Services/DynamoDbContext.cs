using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.DependencyInjection;

namespace Restaurant.Infrastructure.Services;

public static class DynamoDbServiceExtensions
{
    public static IServiceCollection AddDynamoDb(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient());
        services.AddSingleton<IDynamoDBContext, DynamoDBContext>(sp =>
        {
            var client = sp.GetRequiredService<IAmazonDynamoDB>();
            return new DynamoDBContext(client);
        });
        return services;
    }
}