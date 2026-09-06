using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Configs;
using Shared.Events;

namespace EventsLogger.Consumers;

public class Consumer : IConsumer
{
    private readonly string _rabbitHost;
    private readonly int _rabbitPort;
    private readonly string _rabbitUser;
    private readonly string _rabbitPass;
    private readonly string _rabbitExchange;
    private readonly string _similarityRoutingKey;
    private readonly string _rankRoutingKey;
    private readonly string[] _routingKeys;
    private string? _rabbitQueue;
    
    private IChannel? _channel;
    private IConnection? _connection;
    
    public Consumer()
    {
        _rabbitHost = Environment.GetEnvironmentVariable(RabbitMqConfig.Host)!;
        _rabbitPort = int.Parse(Environment.GetEnvironmentVariable(RabbitMqConfig.Port)!);
        _rabbitUser = Environment.GetEnvironmentVariable(RabbitMqConfig.User)!;
        _rabbitPass = Environment.GetEnvironmentVariable(RabbitMqConfig.Password)!;
        _rabbitExchange = Environment.GetEnvironmentVariable(RabbitMqConfig.EventsExchange)!;
        _similarityRoutingKey = Environment.GetEnvironmentVariable(RabbitMqConfig.SimilarityRoutingKey)!;
        _rankRoutingKey = Environment.GetEnvironmentVariable(RabbitMqConfig.RankCalculatedEventRoutingKey)!;

        _routingKeys = new[] { _rankRoutingKey, _similarityRoutingKey };
    }

    public async Task ConnectRabbitMq()
    {
        await InitializeConnectionRabbitMq();
        await InitializeRabbitQueue();
        await InitializeConsumer();

        Console.WriteLine($"EventsLogger успешно подключён к exchange: {_rabbitExchange}");
        Console.WriteLine($"Подписан на события: {string.Join(", ", _routingKeys)}");
    }
    
    public async Task ClearConnections()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
        }
    }
    
    private async Task InitializeRabbitQueue()
    {
        await _channel!.ExchangeDeclareAsync(
            exchange: _rabbitExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
        );

        string queueName = $"events.logger.{Guid.NewGuid()}";

        await _channel.QueueDeclareAsync(
            queue: queueName,
            autoDelete: true
        );

        foreach (string routingKey in _routingKeys)
        {
            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: _rabbitExchange,
                routingKey: routingKey
            );
            
            Console.WriteLine($"Привязан routing key: {routingKey}");
        }

        _rabbitQueue = queueName;
    }

    private async Task InitializeConnectionRabbitMq()
    {
        Console.WriteLine($"Подключение к RabbitMQ: {_rabbitHost}:{_rabbitPort}");

        ConnectionFactory factory = new()
        {
            HostName = _rabbitHost,
            UserName = _rabbitUser,
            Password = _rabbitPass,
            Port = _rabbitPort,
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
    }

    private async Task InitializeConsumer()
    {
        AsyncEventingBasicConsumer consumer = new(_channel!);
        consumer.ReceivedAsync += async (_, ea) => await ProcessMessageAsync(ea);

        await _channel!.BasicConsumeAsync(
            queue: _rabbitQueue!,
            autoAck: false,
            consumer: consumer
        );
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea)
    {
        try
        {
            byte[] body = ea.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);
            
            if (ea.RoutingKey != _rankRoutingKey && ea.RoutingKey!= _similarityRoutingKey)
            {
                Console.WriteLine($"Неизвестный routing key: {ea.RoutingKey}");
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }

            await HandleEvent(
                message,
                ea.DeliveryTag, ea.RoutingKey == _rankRoutingKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки: {ex.Message}");
            if (_channel != null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        }
    }


    private async Task HandleEvent(string message, ulong deliveryTag, bool isRank)
    {
        try
        {
            if (isRank)
            {
                RankCalculatedEvent? @event = JsonSerializer.Deserialize<RankCalculatedEvent>(message);
                if (@event != null)
                {
                    PrintEvent(@event.Type, @event.TextId, @event.Rank, @event.Timestamp);
                }
            }
            else
            {
                SimilarityCalculatedEvent? @event = JsonSerializer.Deserialize<SimilarityCalculatedEvent>(message);
                if (@event != null)
                {
                    PrintEvent(@event.Type, @event.TextId, @event.Similarity, @event.Timestamp);
                }
            }

            await _channel!.BasicAckAsync(deliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке: {ex.Message}");
            throw;
        }
    }

    private void PrintEvent(string type, string id, double value, DateTime timestamp)
    {
        Console.WriteLine($"Тип события: {type}");
        Console.WriteLine($"Идентификатор текста: {id}");
        Console.WriteLine($"Значение: {value}");
        Console.WriteLine($"Время события: {timestamp:yyyy-MM-dd HH:mm:ss}");
    }
}