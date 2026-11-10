using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Matching databases pause when idle. The first connection to a paused database waits until it resumes,
/// and with a long Connection Timeout in the connection string a test can hang on it for many minutes.
/// So we wake the active matching database (the only one tests use) with short connection attempts and retries.
/// </summary>
internal class MatchingDatabaseWaker
{
    /// <summary>
    /// One connection attempt; a paused database is not ready yet, so we retry instead of waiting longer.
    /// </summary>
    private const int ConnectAttemptTimeoutSeconds = 30;

    private readonly IDbContextFactory<AtlasMatchingContext> _matchingFactory;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;

    public MatchingDatabaseWaker(IServiceProvider provider)
    {
        _matchingFactory = provider.GetRequiredService<IDbContextFactory<AtlasMatchingContext>>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
    }

    public async Task WakeUp()
    {
        // The factory picks the active database (A or B) from the data refresh history
        await using var context = _matchingFactory.CreateDbContext();
        var shortAttempt = new SqlConnectionStringBuilder(context.Database.GetConnectionString())
        {
            ConnectTimeout = ConnectAttemptTimeoutSeconds
        };

        // Opening a connection is enough: it makes a paused database resume
        await _pollyRetry.ExecuteWithRetry(async () =>
        {
            await using var connection = new SqlConnection(shortAttempt.ConnectionString);
            await connection.OpenAsync();
        }, _retry.WakeDatabase, $"Wake active matching database {shortAttempt.InitialCatalog}");
    }
}
