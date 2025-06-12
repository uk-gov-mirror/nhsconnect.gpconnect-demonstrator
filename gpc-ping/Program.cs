using System.IdentityModel.Tokens.Jwt;
using gpc_ping;
using gpc_ping.Validators;
using Microsoft.AspNetCore.Http.Extensions;
using NLog;
using NLog.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Host.UseNLog();

        builder.Services.AddScoped<IValidationCommonValidation, ValidationHelper>();


        try
        {
            var startupLogger = LogManager.Setup()
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

        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Get application logger
        var appLogger = app.Logger;
        appLogger.LogInformation("Application starting up");


        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            appLogger.LogInformation("Development environment detected. OpenAPI enabled.");
        }

        app.UseHttpsRedirection();

        //gpc-ping endpoint responds on gpc-ping/**/** anything after gpc-ping/ and will log path and jwt token and time
        app.MapGet("/gpc-ping/{*path}", (HttpContext context, ILogger<Program> logger,
            IValidationCommonValidation validationHelper, string? path) =>
        {
            logger.LogInformation($"Request received at {DateTime.UtcNow}");
            logger.LogInformation($"Request URL: {context.Request.GetDisplayUrl()}");
            logger.LogInformation("Request path: {path}", path);

            string? authorizationHeader = context.Request.Headers.Authorization;
            var version = context.Request.Query["version"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(version))
            {
                return Results.BadRequest(new
                {
                    message =
                        $"You must specify a version of the API you're wishing to validate against, valid values are '{StaticValues.SupportedApiVersions}"
                });
            }

            if (string.IsNullOrEmpty(authorizationHeader))
            {
                logger.LogWarning("No Authorization header found in the request");
                return Results.Ok(new { message = "Ping successful, but no JWT token provided" });
            }

            var token = authorizationHeader;
            if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorizationHeader[7..];
            }

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token)) return Results.BadRequest(new { message = "Ping failed" });

            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Invalid JWT token format");
                return Results.BadRequest(new { message = "Invalid JWT token - unable to read" });
            }

            BaseValidator validator = version switch
            {
                "v0.7.4" => new V074Validator(jwtToken, validationHelper),
                "v1.2.7" => new V127Validator(jwtToken, validationHelper),
                "v1.5.0" => new V150Validator(jwtToken, validationHelper),
                "v1.6.0" => new V160Validator(jwtToken, validationHelper),
                _ => throw new NotImplementedException($"Version {version} is not supported")
            };

            var (isValid, messages) = validator.Validate();


            var message = string.Join(Environment.NewLine, messages);

            return isValid
                ? Results.BadRequest(new { message })
                : Results.Ok(new { message = $"Success - {message.Trim()}" });
        });
    }
}