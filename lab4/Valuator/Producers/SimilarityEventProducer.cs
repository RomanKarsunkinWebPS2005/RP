using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Events;

namespace Valuator.Producers;

public class SimilarityEventProducer : ISimilarityEventProducer
{
    private readonly string _eventsExchange;
    private readonly string _similarityRoutingKey;
    private readonly string _rabbitHost;
    private readonly int _rabbitPort;
    private readonly string _rabbitUser;
    private readonly string _rabbitPass;
    
    private readonly ILogger<SimilarityEventProducer> _logger;

    public SimilarityEventProducer(ILogger<SimilarityEventProducer> logger)
    {
        _logger = logger;
        _eventsExchange = Environment.GetEnvironmentVariable("RABBITMQ_EVENTS_EXCHANGE")!;
        _similarityRoutingKey = Environment.GetEnvironmentVariable("RABBITMQ_SIMILARITY_ROUTING_KEY")!;
        _rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")!;
        _rabbitPort = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT")!);
        _rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USER")!;
        _rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")!;
    }

    public async Task PublishSimilarityEventAsync(string textId, 
        double similarityValue, 
        CancellationToken cancellationToken = default)
    {
        SimilarityCalculatedEvent similarityEvent = new SimilarityCalculatedEvent(textId, similarityValue);
        string message = JsonSerializer.Serialize(similarityEvent);
        
        await PublishMessageAsync(message, cancellationToken);
    }

    private async Task PublishMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        (IConnection connection, IChannel channel) = await InitializeConnection(cancellationToken);

        try
        {
            byte[] messageData = Encoding.UTF8.GetBytes(message);

            await channel.ExchangeDeclareAsync(
                exchange: _eventsExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken
            );

            _logger.LogDebug("Отправка события: {Message}", message);
            await channel.BasicPublishAsync(
                exchange: _eventsExchange,
                routingKey: _similarityRoutingKey,
                mandatory: false,
                body: messageData,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Событие SimilarityCalculated успешно отправлено. TextId: {TextId}, Similarity: {Similarity}", 
                JsonSerializer.Deserialize<SimilarityCalculatedEvent>(message)?.TextId,
                JsonSerializer.Deserialize<SimilarityCalculatedEvent>(message)?.Similarity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке события SimilarityCalculated");
            throw;
        }
        finally
        {
            await channel.CloseAsync(cancellationToken);
            await connection.CloseAsync(cancellationToken);
        }
    }
    
    private async Task<(IConnection connection, IChannel channel)> InitializeConnection(CancellationToken cancellationToken)
    {
        IConnection? connection = null;
        IChannel? channel = null;
        try
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = _rabbitHost,
                Port = _rabbitPort,
                UserName = _rabbitUser,
                Password = _rabbitPass,
                AutomaticRecoveryEnabled = false,
                TopologyRecoveryEnabled = false
            };
        
            connection = await factory.CreateConnectionAsync(cancellationToken);
            channel = await connection.CreateChannelAsync(null, cancellationToken);

            return (connection, channel);
        }
        catch
        {
            if (channel != null)
            {
                await channel.DisposeAsync();
            }

            if (connection != null)
            {
                await connection.DisposeAsync();
            }

            throw;
        }
    }
}