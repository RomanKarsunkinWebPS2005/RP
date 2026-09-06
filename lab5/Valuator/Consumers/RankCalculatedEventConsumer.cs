using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Configs;
using Shared.Events;
using Valuator.Hubs;

namespace Valuator.Consumers;

public class RankCalculatedEventConsumer : IHostedService
{
    private readonly IHubContext<SummaryHub> _hubContext;
    private readonly ILogger<RankCalculatedEventConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    
    private readonly string _rabbitHost;
    private readonly int _rabbitPort;
    private readonly string _rabbitUser;
    private readonly string _rabbitPass;
    private readonly string _eventsExchange;
    private readonly string _rankRoutingKey;
    private readonly string _queueName;

    public RankCalculatedEventConsumer(IHubContext<SummaryHub> hubContext, ILogger<RankCalculatedEventConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        
        _rabbitHost = Environment.GetEnvironmentVariable(RabbitMqConfig.Host)!;
        _rabbitPort = int.Parse(Environment.GetEnvironmentVariable(RabbitMqConfig.Port)!);
        _rabbitUser = Environment.GetEnvironmentVariable(RabbitMqConfig.User)!;
        _rabbitPass = Environment.GetEnvironmentVariable(RabbitMqConfig.Password)!;
        _eventsExchange = Environment.GetEnvironmentVariable(RabbitMqConfig.EventsExchange)!;
        _rankRoutingKey = Environment.GetEnvironmentVariable(RabbitMqConfig.RankCalculatedEventRoutingKey)!;
        _queueName = $"valuator.rank.{Guid.NewGuid()}";
        
        _logger.LogInformation("RankCalculatedEventConsumer инициализирован:");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConnectRabbitMq(cancellationToken);
        await InitializeQueue(cancellationToken);
        await StartConsumer(cancellationToken);
        
        _logger.LogInformation("RankCalculatedEventConsumer успешно запущен и ожидает сообщения");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
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
        
        _logger.LogInformation("RankCalculatedEventConsumer остановлен");
    }

    private async Task ConnectRabbitMq(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Подключение к RabbitMQ: {Host}:{Port}", _rabbitHost, _rabbitPort);

        ConnectionFactory factory = new()
        {
            HostName = _rabbitHost,
            UserName = _rabbitUser,
            Password = _rabbitPass,
            Port = _rabbitPort,
            AutomaticRecoveryEnabled = true
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(null, cancellationToken);

        _logger.LogInformation("Подключено к RabbitMQ, exchange: {Exchange}", _eventsExchange);
    }

    private async Task InitializeQueue(CancellationToken cancellationToken)
    {
        await _channel!.ExchangeDeclareAsync(
            exchange: _eventsExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: _queueName,
            exchange: _eventsExchange,
            routingKey: _rankRoutingKey,
            cancellationToken: cancellationToken
        );
        
        _logger.LogInformation("Очередь привязана: {Queue} <- Exchange:{Exchange} [RoutingKey: {RoutingKey}]",
            _queueName, _eventsExchange, _rankRoutingKey);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
    }

    private async Task StartConsumer(CancellationToken cancellationToken)
    {
        AsyncEventingBasicConsumer consumer = new(_channel!);
        consumer.ReceivedAsync += async (_, ea) => await ProcessMessageAsync(ea, cancellationToken);

        await _channel!.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        try
        {
            byte[] body = ea.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Получено сообщение: {Message}", message);

            RankCalculatedEvent? rankEvent = JsonSerializer.Deserialize<RankCalculatedEvent>(message);

            if (rankEvent == null)
            {
                _logger.LogError("Не удалось десериализовать RankCalculatedEvent");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                return;
            }

            await _hubContext.Clients.Group($"Text_{rankEvent.TextId}")
                .SendAsync("RankReady", rankEvent.TextId, rankEvent.Rank, cancellationToken: cancellationToken);

            await _channel!.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            _logger.LogInformation("Уведомление SignalR отправлено для текста: {TextId}", rankEvent.TextId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки RankCalculatedEvent");
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
        }
    }
}
