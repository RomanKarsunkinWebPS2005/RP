using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Configs;
using Shared.Events;

namespace RankCalculator.Producers;

public class EventProducerService : IEventProducerService
{
    private readonly string _eventsExchange = Environment.GetEnvironmentVariable(RabbitMqConfig.EventsExchange)!;
    private readonly string _rankRoutingKey = Environment.GetEnvironmentVariable(RabbitMqConfig.RankCalculatedEventRoutingKey)!;
    private readonly string _rabbitHost = Environment.GetEnvironmentVariable(RabbitMqConfig.Host)!;
    private readonly int _rabbitPort = int.Parse(Environment.GetEnvironmentVariable(RabbitMqConfig.Port)!);
    private readonly string _rabbitUser = Environment.GetEnvironmentVariable(RabbitMqConfig.User)!;
    private readonly string _rabbitPass = Environment.GetEnvironmentVariable(RabbitMqConfig.Password)!;
    
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task ConnectRabbitMq(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Подключение к RabbitMQ: {_rabbitHost}:{_rabbitPort}");

        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = _rabbitHost,
            Port = _rabbitPort,
            UserName = _rabbitUser,
            Password = _rabbitPass,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(null, cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _eventsExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );

        Console.WriteLine($"Подключено к RabbitMQ, exchange: {_eventsExchange}");
    }

    public async Task PublishRankEventAsync(string textId, double rankValue, CancellationToken cancellationToken = default)
    {
        RankCalculatedEvent rankEvent = new RankCalculatedEvent(textId, rankValue);
        string message = JsonSerializer.Serialize(rankEvent);
        
        await PublishMessageAsync(message, cancellationToken);
    }

    private async Task PublishMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        byte[] messageData = Encoding.UTF8.GetBytes(message);

        await _channel!.BasicPublishAsync(
            exchange: _eventsExchange,
            routingKey: _rankRoutingKey,
            mandatory: false,
            body: messageData,
            cancellationToken: cancellationToken
        );
    }

    public async Task ClearConnections(CancellationToken cancellationToken = default)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection != null && _connection.IsOpen)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }
    }
}