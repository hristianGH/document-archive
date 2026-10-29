using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Export.Api.Errors;
using Export.Core.Clients;
using Export.Core.Handlers;
using Export.Data.Clients;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Export.Api.Startup;

public static class ServiceRegistration
{
    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        // The trailing slash keeps a path in the base URL when relative paths are appended.
        if (!Uri.TryCreate(builder.Configuration["Archive:BaseUrl"]?.TrimEnd('/') + "/", UriKind.Absolute, out var archiveBaseUrl)
            || archiveBaseUrl.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Archive base URL is missing or not an http(s) URL. Set Archive__BaseUrl.");
        }

        var maxRetries = builder.Configuration.GetValue<int>("Archive:MaxRetries");
        if (maxRetries < 1
            || !TimeSpan.TryParse(builder.Configuration["Archive:RetryDelay"], CultureInfo.InvariantCulture, out var retryDelay)
            || retryDelay < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Archive retry settings are missing or invalid. Set Archive__MaxRetries (1 or more) and Archive__RetryDelay (hh:mm:ss).");
        }

        if (!TimeSpan.TryParse(builder.Configuration["Archive:AttemptTimeout"], CultureInfo.InvariantCulture, out var attemptTimeout)
            || attemptTimeout <= TimeSpan.Zero
            || !TimeSpan.TryParse(builder.Configuration["Archive:ContentTimeout"], CultureInfo.InvariantCulture, out var contentTimeout)
            || contentTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Archive timeouts are missing or invalid. Set Archive__AttemptTimeout and Archive__ContentTimeout (hh:mm:ss, above zero).");
        }

        var services = builder.Services;

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new ArchiveClientSettings { ContentTimeout = contentTimeout });

        services.AddHttpClient<IArchiveClient, ArchiveClient>(client => client.BaseAddress = archiveBaseUrl)
            .AddResilienceHandler("archive", pipeline => pipeline
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = maxRetries,
                    Delay = retryDelay,
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false
                })
                .AddTimeout(attemptTimeout));
        services.AddScoped<IExportHandler, ExportHandler>();

        services.AddProblemDetails();
        services.AddExceptionHandler<ProblemExceptionHandler>();

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Length == 0)
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins(allowedOrigins);
            }

            policy.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition");
        }));

        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.AddOpenApi();
        services.AddHealthChecks();

        return builder;
    }
}