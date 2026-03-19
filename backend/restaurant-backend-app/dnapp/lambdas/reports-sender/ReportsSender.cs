using Amazon.Lambda.Core;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Threading.Tasks;
using System.Collections.Generic;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Restaurant.ReportsSender;

public class ReportsSender
{
    private readonly IAmazonSimpleEmailService _ses;
    private readonly string _toEmail;
    private readonly string _fromEmail;

    public ReportsSender()
    {
        _ses = new AmazonSimpleEmailServiceClient();
        _toEmail = "yevhenii.razinkov@nure.ua";
        _fromEmail = "yevhenii.razinkov@nure.ua";
    }

    public async Task FunctionHandler(CloudWatchEvent<object> @event, ILambdaContext context)
    {
        context.Logger.LogInformation("Weekly report triggered");

        var request = new SendEmailRequest
        {
            Source = _fromEmail,
            Destination = new Destination { ToAddresses = new List<string> { _toEmail } },
            Message = new Message
            {
                Subject = new Content("Weekly Restaurant Report"),
                Body = new Body
                {
                    Text = new Content("Weekly report placeholder. Real data coming soon.")
                }
            }
        };

        await _ses.SendEmailAsync(request);
        context.Logger.LogInformation("Report email sent successfully");
    }
}