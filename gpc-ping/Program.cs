using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using GpcPing.Extensions;

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
        Logger startupLogger = null;

        try
        {
            // Setup NLog
            startupLogger = LogManager.Setup()
                .LoadConfigurationFromFile("nlog.config")
                .GetCurrentClassLogger();

            startupLogger.Info("NLog initialized successfully");
        }
        catch (Exception ex)
        {
            // Fallback to console if NLog fails to initialize
            Console.Error.WriteLine($"Error initializing NLog: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure NLog for ASP.NET Core
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // Add services to the container.
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Get application logger
            var appLogger = app.Logger;
            appLogger.LogInformation("Application starting up");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                appLogger.LogInformation("Development environment detected. OpenAPI enabled.");
            }

            app.UseHttpsRedirection();

            //gpc-ping endpoint responds on gpc-ping/**/** anything after gpc-ping/ and will log path and jwt token and time
            app.MapGet("/gpc-ping/{*path}", (HttpContext context, ILogger<Program> logger, string? path) =>
            {
                logger.LogInformation($"Request received at {DateTime.UtcNow}");
                logger.LogInformation($"Request URL: {context.Request.GetDisplayUrl()}");
                logger.LogInformation("Request path: {path}", path);

                // Extract JWT token from Authorization header
                string? authorizationHeader = context.Request.Headers.Authorization;

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    logger.LogWarning("No Authorization header found in the request");
                    return Results.Ok(new { message = "Ping successful, but no JWT token provided" });
                }

                // Bearer token format: "Bearer [token]"
                string token = authorizationHeader;
                if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authorizationHeader.Substring(7);
                }

                try
                {
                    logger.LogInformation("----------------JWT Start----------------");
                    // Decode JWT token
                    var handler = new JwtSecurityTokenHandler();

                    if (handler.CanReadToken(token))
                    {
                        var jwtToken = handler.ReadJwtToken(token);

                        var issues = jwtToken.ValidateToken(logger, context.Request.Method);
                        
                        return Results.Ok(new
                        {
                            message = $"Ping successful, JWT token decoded and logged",
                            header = jwtToken.Header,
                            issuer = jwtToken.Issuer,
                            validTo = jwtToken.ValidTo,
                            validFrom = jwtToken.ValidFrom,
                            claims = jwtToken.Claims.ToDictionary(f => f.Type, f => f.Value),
                            issues,
                        });
                    }
                    else
                    {
                        logger.LogWarning("Invalid JWT token format");
                        return Results.BadRequest(new { message = "Invalid JWT token format" });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error decoding JWT token");
                    return Results.BadRequest(new { message = "Error decoding JWT token", error = ex.Message });
                }
            }).WithName("GpcPing");

            appLogger.LogInformation("Application started and endpoints configured. Ready to serve requests.");

            app.Run();
        }
        catch (Exception ex)
        {
            // Log the exception
            if (startupLogger != null)
            {
                startupLogger.Error(ex, "Application terminated unexpectedly");
            }
            else
            {
                Console.Error.WriteLine($"Application terminated unexpectedly: {ex.Message}");
            }
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit
            LogManager.Shutdown();
        }
    }
}

