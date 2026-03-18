using Xunit;

namespace Restaurant.IntegrationTests.Infrastructure;

[CollectionDefinition("DynamoDb collection", DisableParallelization = true)]
public sealed class DynamoDbCollection : ICollectionFixture<DynamoDbFixture>
{
}