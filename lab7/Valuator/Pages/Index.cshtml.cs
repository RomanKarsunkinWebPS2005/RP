using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared;
using StackExchange.Redis;
using Valuator.Producers;

namespace Valuator.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _database;
    private readonly IProducerService _producerService;
    private readonly ISimilarityEventProducer _similarityEventProducer;

    public IndexModel( ILogger<IndexModel> logger,
        IDatabase database,
        IProducerService producerService,
        ISimilarityEventProducer similarityEventProducer)
    {
        _logger = logger;
        _database = database;
        _producerService = producerService;
        _similarityEventProducer = similarityEventProducer;
    }

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPostAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Page();
        }

        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();
        string userId = await GetCurrentUserIdAsync();

        await StoreUserTextAsync(id, text, userId);
        await PublishRankTaskAsync(id);
        await PublishTextSimilarityAsync(id, text);

        return Redirect($"summary?id={id}");
    }

    private Task<string> GetCurrentUserIdAsync()
    {
        try
        {
            if (!User.Identity!.IsAuthenticated)
            {
                _logger.LogWarning("User is not authenticated");
                throw new UnauthorizedAccessException();
            }

            string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException();
            }

            _logger.LogInformation($"Current authenticated user: {userIdClaim}");
            return Task.FromResult(userIdClaim);
        }
        catch (Exception exception)
        {
            return Task.FromException<string>(exception);
        }
    }

    private async Task StoreUserTextAsync(string id, string text, string userId)
    {
        string textKey = "TEXT-" + id;
        await _database.StringSetAsync(textKey, text);

        string userKey = $"TEXT-USER-{id}";
        await _database.StringSetAsync(userKey, userId);
    }

    private async Task PublishRankTaskAsync(string id)
    {
        string rankKey = "RANK-" + id;

        RankTask rankTask = new RankTask
        {
            Id = id,
            TextKey = "TEXT-" + id,
            RankKey = rankKey,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        await _producerService.PublishMessageAsync(JsonSerializer.Serialize(rankTask));
    }

    private async Task PublishTextSimilarityAsync(string id, string text)
    {
        bool isNewText = await _database.SetAddAsync("UNIQUE-TEXTS", text);
        string similarityKey = "SIMILARITY-" + id;

        await _database.StringSetAsync(similarityKey, isNewText ? "0" : "1");
        await _similarityEventProducer.PublishSimilarityEventAsync(similarityKey, isNewText ? 0 : 1);
    }
}
