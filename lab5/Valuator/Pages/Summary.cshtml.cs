using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IDatabase _database;

    public SummaryModel(ILogger<SummaryModel> logger, IDatabase database)
    {
        _logger = logger;
        _database = database;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }
    public string TextId { get; set; } = string.Empty;

    public async Task OnGet(string id)
    {
        _logger.LogDebug(id);
        TextId = id;

        RedisValue rankResult = await _database.StringGetAsync($"RANK-{id}");
        if (rankResult.HasValue && double.TryParse(rankResult.ToString(), out double rank))
        {
            Rank = rank;
        }
        
        RedisValue similarityResult = await _database.StringGetAsync($"SIMILARITY-{id}");
        if (similarityResult.HasValue && double.TryParse(similarityResult.ToString(), out double similarity))
        {
            Similarity = similarity;
        }
    }
}
