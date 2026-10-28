using Archive.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Archive.Api.Startup;

public class DatabaseMigrator(IServiceScopeFactory scopeFactory, ILogger<DatabaseMigrator> logger) : IHostedService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArchiveDbContext>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations applied");
                return;
            }
            catch (Exception ex) when (ex is RetryLimitExceededException or NpgsqlException { IsTransient: true })
            {
                logger.LogWarning("Database not reachable for migrations (attempt {Attempt}): {Message}. Retrying in {Delay}.",
                    attempt, ex.Message, RetryDelay);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
