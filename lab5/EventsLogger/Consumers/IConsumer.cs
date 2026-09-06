namespace EventsLogger.Consumers;

public interface IConsumer
{
    public Task ConnectRabbitMq();
    public Task ClearConnections();
}