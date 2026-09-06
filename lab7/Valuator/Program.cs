using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Valuator.Producers;
using Valuator.Services;

namespace Valuator;

public static class  Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton(_ =>
        {
            string redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION")!;
            string redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD")!;
            
            string connectionString = redisConnection;
            if (!string.IsNullOrEmpty(redisPassword))
            {
                connectionString += $",password={redisPassword}";
            }
            connectionString += ",abortConnect=false";
            
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);
            return redis.GetDatabase();
        });

        string redisConnectionForDataProtection = Environment.GetEnvironmentVariable("REDIS_CONNECTION")!;
        string redisPasswordForDataProtection = Environment.GetEnvironmentVariable("REDIS_PASSWORD")!;
        
        string dataProtectionConnectionString = redisConnectionForDataProtection;
        if (!string.IsNullOrEmpty(redisPasswordForDataProtection))
        {
            dataProtectionConnectionString += $",password={redisPasswordForDataProtection}";
        }
        dataProtectionConnectionString += ",abortConnect=false";
        
        ConnectionMultiplexer redisMultiplexerForDataProtection = ConnectionMultiplexer.Connect(dataProtectionConnectionString);
        
        builder.Services.AddDataProtection()
            .SetApplicationName("Valuator")
            .PersistKeysToStackExchangeRedis(redisMultiplexerForDataProtection, "DataProtection-Keys");

        builder.Services.AddScoped<UserService>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    context.Response.Redirect("/Account/Login");
                    return Task.CompletedTask;
                }
            };
        });

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
        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapControllers();
        app.UseStatusCodePagesWithReExecute("/Error/{0}");

        app.Run();
    }
}