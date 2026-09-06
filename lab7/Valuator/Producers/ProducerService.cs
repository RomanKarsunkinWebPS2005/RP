using System.Text;
using RabbitMQ.Client;
using Shared.Configs;

namespace Valuator.Producers;

public class ProducerService : IProducerService
{
    private readonly string _exchangeName;
    private readonly string _routingKey;
    private readonly string _rabbitHost;
    private readonly int _rabbitPort;
    private readonly string _rabbitPass;
    private readonly string _rabbitUser;

    private readonly ILogger<ProducerService> _logger;
    
    public ProducerService( ILogger<ProducerService> logger)
    {
        _logger = logger;
        _exchangeName = Environment.GetEnvironmentVariable(RabbitMqConfig.ValuatorExchange)!;
        _routingKey = Environment.GetEnvironmentVariable(RabbitMqConfig.ValuatorRoutingKey)!;
        _rabbitHost = Environment.GetEnvironmentVariable(RabbitMqConfig.Host)!;
        _rabbitPort = int.Parse(Environment.GetEnvironmentVariable(RabbitMqConfig.Port)!);
        _rabbitUser = Environment.GetEnvironmentVariable(RabbitMqConfig.User)!;
        _rabbitPass = Environment.GetEnvironmentVariable(RabbitMqConfig.Password)!;
    }

    public async Task PublishMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        (IConnection connection, IChannel channel) = await InitializeConnection(cancellationToken);

        try
        {
            byte[] messageData = Encoding.UTF8.GetBytes(message);

            _logger.LogDebug("Отправка сообщения: {Message}", message);
            
            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: _routingKey,                   
                mandatory: false,
                body: messageData,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Сообщение успешно отправлено и подтверждено RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке сообщения");
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