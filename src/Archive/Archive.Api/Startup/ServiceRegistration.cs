using System.Text.Json;
using System.Text.Json.Serialization;
using Archive.Api.Cabinets;
using Archive.Api.Errors;
using Archive.Api.HealthChecks;
using Archive.Core.Handlers;
using Archive.Core.Repositories;
using Archive.Data;
using Archive.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Archive.Api.Startup;

public static class ServiceRegistration
{
    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Archive")
            ?? throw new InvalidOperationException("Connection string 'Archive' is missing. Set ConnectionStrings__Archive.");

        var cabinetsPath = builder.Configuration["Cabinets:Path"]
            ?? throw new InvalidOperationException("Cabinets file path is missing. Set Cabinets__Path.");

        var services = builder.Services;

        services.AddSingleton(CabinetFileLoader.Load(Path.Combine(builder.Environment.ContentRootPath, cabinetsPath)));

        services.AddDbContext<ArchiveDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
        services.AddHostedService<DatabaseMigrator>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentHandler, DocumentHandler>();

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

            policy.AllowAnyHeader().AllowAnyMethod();
        }));

        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.AddOpenApi();
        services.AddHealthChecks()
            .AddCheck("database", new DatabaseHealthCheck(connectionString), tags: ["ready"], timeout: TimeSpan.FromSeconds(3));

        return builder;
    }
}
