namespace Restaurant.Core.Interfaces.Services;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default);
}
