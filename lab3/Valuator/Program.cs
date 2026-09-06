using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Valuator.Services;

namespace Valuator;

public class  Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton( sp =>
        {
            string? configuration = builder.Configuration.GetConnectionString( "Redis" );
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect( configuration );
            return redis.GetDatabase();
        } );

        builder.Services.AddRazorPages( options =>
        {
            options.Conventions.ConfigureFilter( new IgnoreAntiforgeryTokenAttribute() );
        } );
        builder.Services.AddScoped<IProducerService, ProducerService>();  
        WebApplication app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
