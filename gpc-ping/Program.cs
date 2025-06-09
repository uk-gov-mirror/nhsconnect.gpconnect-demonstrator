using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

public partial class Program
{
    public static void Main(string[] args)
    {
        // Logger startupLogger = null;
        //
        // try
        // {
        //     // Setup NLog
        //     startupLogger = LogManager.Setup()
        //         .LoadConfigurationFromFile("nlog.config")
        //         .GetCurrentClassLogger();
        //
        //     startupLogger.Info("NLog initialized successfully");
        // }
        // catch (Exception ex)
        // {
        //     // Fallback to console if NLog fails to initialize
        //     Console.Error.WriteLine($"Error initializing NLog: {ex.Message}");
        //     if (ex.InnerException != null)
        //     {
        //         Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
        //     }
        // }
        //
        // try
        // {
        //     var builder = WebApplication.CreateBuilder(args);
        //
        //     // Configure NLog for ASP.NET Core
        //     builder.Logging.ClearProviders();
        //     builder.Host.UseNLog();
        //
        //     // Add services to the container.
        //     // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        //     builder.Services.AddOpenApi();
        //
        //     var app = builder.Build();
        //
        //     // Get application logger
        //     var appLogger = app.Logger;
        //     appLogger.LogInformation("Application starting up");
        //
        //     // Configure the HTTP request pipeline.
        //     if (app.Environment.IsDevelopment())
        //     {
        //         app.MapOpenApi();
        //         appLogger.LogInformation("Development environment detected. OpenAPI enabled.");
        //     }
        //
        //     app.UseHttpsRedirection();
        //
        //     //gpc-ping endpoint responds on gpc-ping/**/** anything after gpc-ping/ and will log path and jwt token and time
        //     app.MapGet("/gpc-ping/{*path}", (HttpContext context, ILogger<Program> logger, string? path) =>
        //     {
        //         logger.LogInformation($"Request received at {DateTime.UtcNow}");
        //         logger.LogInformation($"Request URL: {context.Request.GetDisplayUrl()}");
        //         logger.LogInformation("Request path: {path}", path);
        //     }
        // }
        // catch (Exception ex)
        // {
        //     return BadRequest();
        // }
    }
}