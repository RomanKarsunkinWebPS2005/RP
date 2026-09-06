using EventsLogger.Consumers;

namespace EventsLogger;

public static class Program
{
    private static IConsumer _consumer = new Consumer();
    private static CancellationTokenSource _cts = new();

    static async Task Main()
    {
        try
        {
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
        await _consumer.ClearConnections();

        Console.WriteLine("RankCalculator остановлен");
    }
}