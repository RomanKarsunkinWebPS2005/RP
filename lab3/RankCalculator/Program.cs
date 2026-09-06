using System.Text;
using StackExchange.Redis;

namespace RankCalculator;

public static class Program
{
    private static ConnectionMultiplexer? _redis;
    private static IDatabase? _redisDb;
    private static IConsumer? _consumer;
    private static CancellationTokenSource _cts = new();

    static async Task Main()
    {
        try
        {
            await ConnectRedis();

            _consumer = new Consumer(_redisDb!);
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
    
    private static async Task ClearConnections()
    {
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