using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _database;

    public IndexModel( ILogger<IndexModel> logger, IDatabase database )
    {
        _logger = logger;
        _database = database;
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
        // TODO: (pa1) сохранить в БД (Redis) text по ключу textKey

        string rankKey = "RANK-" + id;
        double rank = CalculateRank( text );
        await _database.StringSetAsync( rankKey, rank.ToString() );

        // TODO: (pa1) посчитать rank и сохранить в БД (Redis) по ключу rankKey
        bool isNewText = await _database.SetAddAsync( "UNIQUE-TEXTS", text );


        string similarityKey = "SIMILARITY-" + id;
        await _database.StringSetAsync( similarityKey, isNewText ? "0" : "1" );
        // TODO: (pa1) посчитать similarity и сохранить в БД (Redis) по ключу similarityKey

        return Redirect( $"summary?id={id}" );
    }

    private double CalculateRank( string text )
    {
        if ( string.IsNullOrEmpty( text ) )
        {
            return 0;
        }

        int totalChars = text.Length;
        int nonAlphabetChars = 0;

        foreach ( char c in text )
        {
            bool isAlphabetic =
                ( c >= 'A' && c <= 'Z' ) ||  
                ( c >= 'a' && c <= 'z' ) ||
                ( c >= 'А' && c <= 'Я' ) ||  
                ( c >= 'а' && c <= 'я' ) ||
                c == 'Ё' || c == 'ё';    

            if ( !isAlphabetic )
            {
                nonAlphabetChars++;
            }
        }

        return ( double )nonAlphabetChars / totalChars;
    }
}
