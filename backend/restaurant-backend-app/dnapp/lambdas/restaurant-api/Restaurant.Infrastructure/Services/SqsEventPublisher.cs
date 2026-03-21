using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Restaurant.Core.Interfaces.Services;
using System.Text.Json;

namespace Restaurant.Infrastructure.Services;

public class SqsEventPublisher : IEventPublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsEventPublisher(IAmazonSQS sqsClient, IConfiguration config)
    {
        _sqsClient = sqsClient;
        _queueUrl = config["Sqs:QueueUrl"] ?? config["SQS_QUEUE_URL"]
            ?? throw new InvalidOperationException("SQS queue URL not configured");
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(@event);

        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = body
        };

        await _sqsClient.SendMessageAsync(request, ct);
    }
}
