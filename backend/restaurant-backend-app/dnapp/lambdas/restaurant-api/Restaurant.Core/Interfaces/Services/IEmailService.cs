namespace Restaurant.Core.Interfaces.Services;

public interface IEmailService
{
    Task SendEmail(string message, string subject, string email, CancellationToken ct);
}