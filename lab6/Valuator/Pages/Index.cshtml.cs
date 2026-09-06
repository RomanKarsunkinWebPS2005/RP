using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared;
using Shared.Enums;
using Shared.Helpers;
using StackExchange.Redis;
using Valuator.Producers;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IProducerService _producerService;
    private readonly ISimilarityEventProducer _similarityEventProducer;
    private readonly RedisService _redisService;

    public IndexModel( ILogger<IndexModel> logger,
        IProducerService producerService,
        ISimilarityEventProducer similarityEventProducer,
        RedisService redisService)
    {
        _logger = logger;
        _producerService = producerService;
        _similarityEventProducer = similarityEventProducer;
        _redisService = redisService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string text, string country)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(country))
        {
            return Page();
        }

        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();
        Enum.TryParse(country, true, out Country countryEnum);
        Region region = CountryRegionHelper.GetRegion(countryEnum);
        string regionCode = CountryRegionHelper.GetRegionCode(region);

        _logger.LogInformation($"LOOKUP: {id}, {regionCode}");

        await SetShardMappingAsync(id, regionCode);
        await SetTextInRegionalDatabaseAsync(id, text, region);
    
        bool isNewText = await CheckAndMarkUniqueTextAsync(text);
        await PublishSimilarityAsync(id, isNewText, region);
    
        await PublishRankTaskAsync(id, countryEnum);

        return Redirect($"summary?id={id}&region={regionCode}");
    }
    
    private async Task SetShardMappingAsync(string id, string regionCode)
    {
        string shardMapKey = RedisKeyHelper.CreateShardKey(id);
        IDatabase mainDatabase = _redisService.GetMainDatabase();
        await mainDatabase.StringSetAsync(shardMapKey, regionCode);
    }

    private async Task SetTextInRegionalDatabaseAsync(string id, string text, Region region)
    {
        string textKey = RedisKeyHelper.CreateTextKey(id);
        IDatabase regionalDatabase = _redisService.GetDatabaseForRegion(region);
        await regionalDatabase.StringSetAsync(textKey, text);
    }

    private async Task PublishRankTaskAsync(string id, Country countryEnum)
    {
        string rankKey = RedisKeyHelper.CreateRankKey(id);
        string textKey = RedisKeyHelper.CreateTextKey(id);
        
        RankTask rankTask = new RankTask
        {
            Id = id,
            TextKey = textKey,
            RankKey = rankKey,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
        };
        
        await _producerService.PublishMessageAsync(JsonSerializer.Serialize(rankTask));
    }

    private async Task<bool> CheckAndMarkUniqueTextAsync(string text)
    {
        IDatabase mainDatabase = _redisService.GetMainDatabase();
        return await mainDatabase.SetAddAsync("UNIQUE-TEXTS", text);
    }

    private async Task PublishSimilarityAsync(string id, bool isNewText, Region region)
    {
        string similarityKey = RedisKeyHelper.CreateSimilarityKey(id);
    
        IDatabase regionalDatabase = _redisService.GetDatabaseForRegion(region);
    
        await regionalDatabase.StringSetAsync(similarityKey, isNewText ? "0" : "1");
        await _similarityEventProducer.PublishSimilarityEventAsync(id, isNewText ? 0 : 1);
    }}