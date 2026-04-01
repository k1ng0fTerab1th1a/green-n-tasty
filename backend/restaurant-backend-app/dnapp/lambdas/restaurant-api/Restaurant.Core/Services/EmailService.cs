using System.Text;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.SharedModels;

namespace Restaurant.Core.Services;

public class EmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _ses;
    private readonly string _fromEmail;

    public EmailService(IAmazonSimpleEmailService ses, EmailServiceSettings settings)
    {
        _ses = ses;
        _fromEmail = settings.FromEmail;
    }
    
    public async Task SendEmail(string message, string subject, string email, CancellationToken ct)
    {
        var boundary = $"boundary_{Guid.NewGuid():N}";
        var sb = new StringBuilder();

        sb.AppendLine($"From: {_fromEmail}");
        sb.AppendLine($"To: {email}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        sb.AppendLine();

        sb.AppendLine($"--{boundary}");
        sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
        sb.AppendLine();
        sb.AppendLine(message);
        sb.AppendLine();

        sb.AppendLine($"--{boundary}--");

        var rawBytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var rawStream = new MemoryStream(rawBytes);

        await _ses.SendRawEmailAsync(new SendRawEmailRequest
        {
            RawMessage = new RawMessage { Data = rawStream }
        }, ct);
    }
}