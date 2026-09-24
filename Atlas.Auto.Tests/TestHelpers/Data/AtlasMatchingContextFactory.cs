using Atlas.Auto.Tests.TestHelpers.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.Data;

internal class AtlasMatchingContextFactory(
    IDbContextFactory<AtlasContext> atlasFactory,
    DatabaseConfig dbConfig,
    ILoggerFactory loggerFactory) : IDbContextFactory<AtlasMatchingContext>
{
    // Cached for the lifetime of the test run; won't pick up an A↔B database switch mid-run.
    private string? _resolvedConnectionString;
    private readonly object _lock = new();

    public AtlasMatchingContext CreateDbContext()
    {
        var connectionString = GetConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<AtlasMatchingContext>();
        optionsBuilder.UseSqlServer(connectionString, dbConfig.ConfigureSqlServer);
        optionsBuilder.UseLoggerFactory(loggerFactory);
        if (dbConfig.DebugPrint) optionsBuilder.EnableSensitiveDataLogging();

        return new AtlasMatchingContext(optionsBuilder.Options);
    }

    private string GetConnectionString()
    {
        if (_resolvedConnectionString != null)
            return _resolvedConnectionString;

        lock (_lock)
        {
            if (_resolvedConnectionString != null)
                return _resolvedConnectionString;

            using var context = atlasFactory.CreateDbContext();
            var activeRecord = context.DataRefreshHistory
                .Where(r => r.RefreshEndUtc != null && r.WasSuccessful == true)
                .OrderByDescending(r => r.RefreshEndUtc)
                .FirstOrDefault();

            var activeDb = activeRecord?.Database
                ?? throw new InvalidOperationException(
                    "No successful data refresh records found in DataRefreshHistory. " +
                    "Cannot determine active matching database.");
            _resolvedConnectionString = activeDb == "DatabaseB" ? dbConfig.SqlB : dbConfig.SqlA;

            return _resolvedConnectionString;
        }
    }
}
