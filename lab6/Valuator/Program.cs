using Microsoft.AspNetCore.Mvc;
using Valuator.Producers;
using Valuator.Services;

namespace Valuator;

public class  Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


        builder.Services.AddSingleton<RedisConnectionFactory>();
        

        builder.Services.AddSingleton<RedisService>();

        builder.Services.AddRazorPages( options =>
        {
            options.Conventions.ConfigureFilter( new IgnoreAntiforgeryTokenAttribute() );
        } );
        
        builder.Services.AddScoped<IProducerService, ProducerService>();  
        builder.Services.AddScoped<ISimilarityEventProducer, SimilarityEventProducer>();
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
