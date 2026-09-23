using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class DatabaseConfig
{
    public string Sql { get; set; } = string.Empty;
    public string SqlA { get; set; } = string.Empty;
    public string SqlB { get; set; } = string.Empty;
    public bool DebugPrint { get; set; }

    public int RetryCount { get; set; } = 5;
    public int RetryIntervalSeconds { get; set; } = 10;
    public int CommandTimeoutSeconds { get; set; } = 90;

    internal void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql)
    {
        sql.EnableRetryOnFailure(RetryCount, TimeSpan.FromSeconds(RetryIntervalSeconds), null);
        sql.CommandTimeout(CommandTimeoutSeconds);
    }
}
