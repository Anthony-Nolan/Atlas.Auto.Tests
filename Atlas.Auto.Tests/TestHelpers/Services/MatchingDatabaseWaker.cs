using Atlas.Auto.Tests.TestHelpers.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Matching databases A and B pause when idle. The first connection to a paused database waits until it resumes,
/// and with a long Connection Timeout in the connection string a test can hang on it for many minutes.
/// So we wake both databases before the tests, with short connection attempts and retries.
/// </summary>
internal class MatchingDatabaseWaker
{
    /// <summary>
    /// One connection attempt; a paused database is not ready yet, so we retry instead of waiting longer.
    /// </summary>
    private const int ConnectAttemptTimeoutSeconds = 30;

    private readonly DatabaseConfig _dbConfig;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;

    public MatchingDatabaseWaker(IServiceProvider provider)
    {
        _dbConfig = provider.GetRequiredService<DatabaseConfig>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
    }

    public async Task WakeUp()
    {
        await Task.WhenAll(
            WakeUp("matching A", _dbConfig.SqlA),
            WakeUp("matching B", _dbConfig.SqlB));
    }

    private async Task WakeUp(string databaseName, string connectionString)
    {
        var shortAttempt = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = ConnectAttemptTimeoutSeconds };

        // Opening a connection is enough: it makes a paused database resume
        await _pollyRetry.ExecuteWithRetry(async () =>
        {
            await using var connection = new SqlConnection(shortAttempt.ConnectionString);
            await connection.OpenAsync();
        }, _retry.WakeDatabase, $"Wake {databaseName} database");
    }
}
