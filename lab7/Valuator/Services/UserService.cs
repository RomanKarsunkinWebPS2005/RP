using StackExchange.Redis;
using Shared;

namespace Valuator.Services;

public class UserService
{
    private readonly IDatabase _database;
    private readonly ILogger<UserService> _logger;

    public UserService(IDatabase database, ILogger<UserService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<bool> CreateUserAsync(User user)
    {
        try
        {
            string key = $"USER-{user.Login}";
            
            if (await _database.KeyExistsAsync(key))
            {
                return false;
            }
            
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            
            await _database.StringSetAsync(key, System.Text.Json.JsonSerializer.Serialize(user));
            
            await _database.SetAddAsync("USERS", user.Login);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Login}", user.Login);
            return false;
        }
    }

    private async Task<User?> GetUserAsync(string login)
    {
        try
        {
            string key = $"USER-{login}";
            string? userData = await _database.StringGetAsync(key);
            
            if (string.IsNullOrEmpty(userData))
            {
                return null;
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<User>(userData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {Login}", login);
            return null;
        }
    }

    public async Task<bool> ValidateUserAsync(string login, string password)
    {
        try
        {
            User? user = await GetUserAsync(login);
            if (user == null)
            {
                Console.WriteLine($"User {login} not found");
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user {Login}", login);
            return false;
        }
    }
}