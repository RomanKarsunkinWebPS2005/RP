namespace RankCalculator;

public interface IConsumer
{
    public Task ConnectRabbitMq();
    public Task ClearConnections();
}