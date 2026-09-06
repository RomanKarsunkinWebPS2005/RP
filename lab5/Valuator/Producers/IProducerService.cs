namespace Valuator.Producers;

public interface IProducerService
{
    Task PublishMessageAsync(string message, CancellationToken cancellationToken = default);
}