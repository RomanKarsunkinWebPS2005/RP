using System.Collections.Concurrent;
using StackExchange.Redis;

namespace RankCalculator.Services;

public class RedisConnectionFactory
{
    private readonly ConcurrentDictionary<string, IDatabase> _databases = new();
    private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections = new();

    public IDatabase GetDatabase(string connectionString)
    {
        if (!_databases.TryGetValue(connectionString, out IDatabase? database))
        {
            ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(connectionString);
            database = connection.GetDatabase();
            _connections[connectionString] = connection;
            _databases[connectionString] = database;
        }
        return database;
    }

    public void Dispose()
    {
        foreach (ConnectionMultiplexer connection in _connections.Values)
        {
            connection.Close();
        }
        _connections.Clear();
        _databases.Clear();
    }
}