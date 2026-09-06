namespace Valuator.Services;

public interface IProducerService
{
    Task PublishMessageAsync(string message, CancellationToken cancellationToken = default);
}