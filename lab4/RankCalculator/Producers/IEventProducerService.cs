namespace RankCalculator.Producers;

public interface IEventProducerService
{
    public Task PublishRankEventAsync(string textId,
        double rankValue,
        CancellationToken cancellationToken = default);

    public Task ConnectRabbitMq(CancellationToken cancellationToken = default);
    public Task ClearConnections(CancellationToken cancellationToken = default);
}