using System.Text;
using RankCalculator.Consumers;
using RankCalculator.Producers;
using StackExchange.Redis;

namespace RankCalculator;

public static class Program
{
    private static ConnectionMultiplexer? _redis;
    private static IDatabase? _redisDb;
    private static IConsumer? _consumer;
    private static IEventProducerService? _eventProducerService;
    private static CancellationTokenSource _cts = new();

    static async Task Main()
    {
        try
        {
            await ConnectRedis();
            _eventProducerService = new EventProducerService();
            await _eventProducerService.ConnectRabbitMq();
            _consumer = new Consumer(_redisDb!, _eventProducerService);
            await _consumer.ConnectRabbitMq();
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при запуске: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
            }
        }
        finally
        {
            await ClearConnections();
        }
    }

    public static async Task ClearConnections()
    {
        if (_eventProducerService != null)
        {
            await _eventProducerService.ClearConnections();
        }

        if (_consumer != null)
        {
            await _consumer.ClearConnections();
        }

        if (_redis != null)
        {
            await _redis.CloseAsync();
        }

        Console.WriteLine("RankCalculator остановлен");
    }

    private static async Task ConnectRedis()
    {
        string redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION")!;

        Console.WriteLine($"Подключение к Redis: {redisConnection}");

        _redis = await ConnectionMultiplexer.ConnectAsync(redisConnection);
        _redisDb = _redis.GetDatabase();
        await _redisDb.PingAsync();
        Console.WriteLine("Подключено к Redis");
    }
}