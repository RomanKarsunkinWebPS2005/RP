using System.Globalization;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;
using StackExchange.Redis;

namespace RankCalculator;

public class Consumer : IConsumer
{
    private readonly IDatabase _redisDb;
    private IChannel? _channel;
    private IConnection? _connection;
    private readonly string _rabbitQueue;
    private readonly string _rabbitHost;
    private readonly int _rabbitPort;
    private readonly string _rabbitUser;
    private readonly string _rabbitPass;
    private readonly string _rabbitExchange;
    private readonly string _routingKey;

    public Consumer(IDatabase redisDb)
    {
        _redisDb = redisDb;

        _rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")!;
        _rabbitPort = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT")!);
        _rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USER")!;
        _rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")!;
        _rabbitQueue = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE")!;
        _rabbitExchange = Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE")!;
        _routingKey = Environment.GetEnvironmentVariable("RABBITMQ_ROUTING_KEY")!;
    }

    public async Task ConnectRabbitMq()
    {
        await InitializeConnectionRabbitMq();
        await InitializeRabbitQueue();
        await InitializeConsumer();

        Console.WriteLine($"Consumer успешно подключён к очереди: {_rabbitQueue}");
    }

    private async Task InitializeRabbitQueue()
    {
        await _channel!.ExchangeDeclareAsync(
            exchange: _rabbitExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
        );

        await _channel.QueueDeclareAsync(
            queue: _rabbitQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        await _channel.QueueBindAsync(
            queue: _rabbitQueue,
            exchange: _rabbitExchange,
            routingKey: _routingKey
        );

        await _channel!.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
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

    private async Task InitializeConnectionRabbitMq()
    {
        Console.WriteLine($"Подключение к RabbitMQ: {_rabbitHost}:{_rabbitPort}");

        ConnectionFactory factory = new()
        {
            HostName = _rabbitHost,
            UserName = _rabbitUser,
            Password = _rabbitPass,
            Port = _rabbitPort,
            AutomaticRecoveryEnabled = true
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    private async Task InitializeConsumer()
    {
        AsyncEventingBasicConsumer consumer = new(_channel!);
        consumer.ReceivedAsync += async (_, ea) => await ProcessMessageAsync(ea);

        await _channel!.BasicConsumeAsync(
            queue: _rabbitQueue,
            autoAck: false,
            consumer: consumer
        );
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea)
    {
        try
        {
            RankTask? task = await ExtractTaskFromMessageAsync(ea);

            if (task == null)
            {
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            await HandleTask(ea, task);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки: {ex.Message}");
            if (_channel != null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        }
    }

    private async Task HandleTask(BasicDeliverEventArgs ea, RankTask task)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Обработка задачи: {task.Id}");
        
        string text = _redisDb.StringGet(task.TextKey)!;
        double rank = CalculateRank(text);
        await _redisDb.StringSetAsync(task.RankKey, rank.ToString(CultureInfo.InvariantCulture));

        await _channel!.BasicAckAsync(ea.DeliveryTag, false);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Готово: {task.Id} → ранг = {rank:F4}");
    }

    private async Task<RankTask?> ExtractTaskFromMessageAsync(BasicDeliverEventArgs ea)
    {
        byte[] body = ea.Body.ToArray();
        string message = Encoding.UTF8.GetString(body);

        RankTask? task = JsonSerializer.Deserialize<RankTask>(message);

        if (task == null)
        {
            Console.WriteLine("Ошибка: не удалось десериализовать сообщение");
        }

        return task;
    }

    private double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int totalChars = text.Length;
        int nonAlphabetChars = 0;

        foreach (char c in text)
        {
            if (!IsAlphabetic(c))
            {
                nonAlphabetChars++;
            }
        }

        return (double)nonAlphabetChars / totalChars;
    }

    private bool IsAlphabetic(char c)
    {
        return (c >= 'A' && c <= 'Z') ||
               (c >= 'a' && c <= 'z') ||
               (c >= 'А' && c <= 'Я') ||
               (c >= 'а' && c <= 'я') ||
               c == 'Ё' || c == 'ё';
    }
}