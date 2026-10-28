using System.Text.Json;
using System.Text.Json.Serialization;
using Archive.Api.Cabinets;
using Archive.Api.Errors;
using Archive.Api.HealthChecks;
using Archive.Core.Handlers;
using Archive.Core.Paging;
using Archive.Core.Repositories;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Archive.Data;
using Archive.Data.Repositories;
using Archive.Data.Storage;
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

        var filesPath = builder.Configuration["Storage:FilesPath"]
            ?? throw new InvalidOperationException("Part files path is missing. Set Storage__FilesPath.");

        var maxPageSize = builder.Configuration.GetValue<int>("Paging:MaxPageSize");
        if (maxPageSize < 1)
        {
            throw new InvalidOperationException("Maximum page size is missing or below 1. Set Paging__MaxPageSize.");
        }

        var uploadSettings = new UploadSettings
        {
            MaxZipBytes = builder.Configuration.GetValue<long>("Upload:MaxZipBytes"),
            MaxUncompressedBytes = builder.Configuration.GetValue<long>("Upload:MaxUncompressedBytes"),
            MaxEntries = builder.Configuration.GetValue<int>("Upload:MaxEntries")
        };
        if (uploadSettings.MaxZipBytes < 1 || uploadSettings.MaxUncompressedBytes < 1 || uploadSettings.MaxEntries < 1)
        {
            throw new InvalidOperationException("Upload limits are missing or below 1. Set Upload__MaxZipBytes, Upload__MaxUncompressedBytes and Upload__MaxEntries.");
        }

        var services = builder.Services;

        services.AddSingleton(CabinetFileLoader.Load(Path.Combine(builder.Environment.ContentRootPath, cabinetsPath)));
        services.AddSingleton<IPartFileStore>(new PartFileStore(Path.GetFullPath(filesPath, builder.Environment.ContentRootPath)));
        services.AddSingleton(new PagingSettings { MaxPageSize = maxPageSize });
        services.AddSingleton(uploadSettings);
        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<ArchiveDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));
        services.AddHostedService<DatabaseMigrator>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentHandler, DocumentHandler>();
        services.AddScoped<IUploadHandler, UploadHandler>();
        services.AddScoped<ICabinetHandler, CabinetHandler>();

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
