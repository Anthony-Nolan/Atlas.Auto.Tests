namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class RetryPolicy
{
    public int RetryCount { get; set; }
    public int IntervalSeconds { get; set; }
}

internal class RetrySettings
{
    public RetryPolicy ApiCall { get; set; } = new() { RetryCount = 5, IntervalSeconds = 5 };
    public RetryPolicy FetchResultSet { get; set; } = new() { RetryCount = 5, IntervalSeconds = 10 };
    public RetryPolicy FetchMessages { get; set; } = new() { RetryCount = 10, IntervalSeconds = 20 };
    public RetryPolicy CheckDonors { get; set; } = new() { RetryCount = 10, IntervalSeconds = 20 };
    public RetryPolicy CheckDonorsInMatching { get; set; } = new() { RetryCount = 45, IntervalSeconds = 20 };
    public RetryPolicy HealthCheck { get; set; } = new() { RetryCount = 3, IntervalSeconds = 20 };
    public RetryPolicy Cleanup { get; set; } = new() { RetryCount = 3, IntervalSeconds = 10 };
    public RetryPolicy TestRetry { get; set; } = new() { RetryCount = 2, IntervalSeconds = 0 };
}
