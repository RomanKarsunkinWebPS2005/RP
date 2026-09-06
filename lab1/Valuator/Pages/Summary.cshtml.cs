using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
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

    public async Task OnGet(string id)
    {
        _logger.LogDebug(id);

        Rank = (double) await _database.StringGetAsync( $"RANK-{id}" );
        Similarity = ( double ) await _database.StringGetAsync( $"SIMILARITY-{id}" );
        // TODO: (pa1) проинициализировать свойства Rank и Similarity значениями из БД (Redis)
    }
}
