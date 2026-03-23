using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Restaurant.ReportsHandler;

public class ReportsHandler
{
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        foreach (var message in sqsEvent.Records)
        {
            context.Logger.LogInformation($"Received message: {message.Body}");
        }
    }
}