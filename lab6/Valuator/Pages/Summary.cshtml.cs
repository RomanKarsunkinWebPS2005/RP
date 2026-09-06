using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared.Helpers;
using StackExchange.Redis;
using Valuator.Services;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly RedisService _redisService;

    public SummaryModel(ILogger<SummaryModel> logger, RedisService redisService)
    {
        _logger = logger;
        _redisService = redisService;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public async Task OnGetAsync(string id, string region)
    {
        _logger.LogDebug("LOOKUP: {Id}, {Region}", id, region);

        IDatabase db = _redisService.GetDatabaseForRegion(CountryRegionHelper.GetRegionByCode(region));
    
        RedisValue rankValue = new RedisValue();
        for (int i = 0; i < 10; i++)
        {
            rankValue = await db.StringGetAsync(RedisKeyHelper.CreateRankKey(id));
            if (rankValue.HasValue)
            {
                _logger.LogInformation($"Rank получен с попытки {i + 1}");
                break;
            }
            
            _logger.LogWarning($"Rank еще не готов, попытка {i + 1}");
            await Task.Delay(100);
        }
    
        RedisValue similarityValue = await db.StringGetAsync(RedisKeyHelper.CreateSimilarityKey(id));
    
        if (rankValue.HasValue && double.TryParse(rankValue.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double rank))
        {
            Rank = rank;
        }
        else
        {
            Rank = 0;
            _logger.LogWarning("Не удалось получить rank для {Id}", id);
        }
    
        if (similarityValue.HasValue && double.TryParse(similarityValue.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double similarity))
        {
            Similarity = similarity;
        }
        else
        {
            Similarity = 0;
            _logger.LogWarning("Не удалось получить similarity для {Id}", id);
        }
    }
}