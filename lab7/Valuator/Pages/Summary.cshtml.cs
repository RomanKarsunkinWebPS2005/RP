using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using Valuator.Services;

namespace Valuator.Pages;

[Authorize]
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IDatabase _database;

    public SummaryModel(ILogger<SummaryModel> logger, IDatabase database, UserService userService)
    {
        _logger = logger;
        _database = database;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        string? textUserId = await _database.StringGetAsync($"TEXT-USER-{id}");
        if (textUserId == null)
        {
            return RedirectToPage("/Error/Unauthorized");
        }

        string currentUserId = GetCurrentUserId();

        bool isAuthor = !string.IsNullOrEmpty(currentUserId) && textUserId == currentUserId;
        
        if (isAuthor)
        {
            await LoadSummaryDataAsync(id);
            
            return Page();
        }
       
        return RedirectToPage("/Account/AccessDenied");
    }

    private string GetCurrentUserId()
    {
        if (!User.Identity!.IsAuthenticated)
        {
            _logger.LogWarning("User is not authenticated");
            return string.Empty;
        }

        string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("Empty user ID claim received");
            return string.Empty;
        }

        _logger.LogInformation($"Current authenticated user: {userIdClaim}");
        
        return userIdClaim;
    }

    private async Task LoadSummaryDataAsync(string id)
    {
        RedisValue rankValue = new RedisValue();
        for (int i = 0; i < 10; i++)
        {
            rankValue = await _database.StringGetAsync($"RANK-{id}");
            if (rankValue.HasValue)
            {
                _logger.LogInformation($"Rank получен с попытки {i + 1}");
                break;
            }
            
            _logger.LogWarning($"Rank еще не готов, попытка {i + 1}");
            await Task.Delay(100);
        }
    
        RedisValue similarityValue = await _database.StringGetAsync($"SIMILARITY-{id}");
    
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