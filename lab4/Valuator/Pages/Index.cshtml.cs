using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared;
using StackExchange.Redis;
using Valuator.Producers;

namespace Valuator.Pages;

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

    public async Task<IActionResult> OnPostAsync( string text )
    {
        if ( string.IsNullOrEmpty( text ) )
        {
            return Page();
        }

        _logger.LogDebug( text );

        string id = Guid.NewGuid().ToString();

        string textKey = "TEXT-" + id;
        await _database.StringSetAsync( textKey, text );

        string rankKey = "RANK-" + id;
        RankTask rankTask = new RankTask
        {
            Id = id,
            TextKey = textKey,
            RankKey = rankKey,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
        await _producerService.PublishMessageAsync(JsonSerializer.Serialize((rankTask)));

        bool isNewText = await _database.SetAddAsync( "UNIQUE-TEXTS", text );
        string similarityKey = "SIMILARITY-" + id;
        await _database.StringSetAsync( similarityKey, isNewText ? "0" : "1" );
        await _similarityEventProducer.PublishSimilarityEventAsync( similarityKey, isNewText ? 0 : 1 );
        return Redirect( $"summary?id={id}" );
    }
}
