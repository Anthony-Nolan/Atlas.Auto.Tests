namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Whether App Insights is storing the telemetry Atlas sends. When it is not - usually because the daily cap has been
/// reached - the events a test waits for never arrive, so the test should be skipped rather than fail.
/// </summary>
/// <remarks>Checked once per test run: each pipeline job is its own run, so a cap reached mid-pipeline is still seen.</remarks>
internal class AppInsightsIngestionCheck
{
    private static readonly TimeSpan MaxRequestAge = TimeSpan.FromMinutes(15);

    private readonly Lazy<Task<string?>> _problem;

    public AppInsightsIngestionCheck(AppInsightsHelper appInsightsHelper)
    {
        _problem = new Lazy<Task<string?>>(() => FindProblem(appInsightsHelper));
    }

    /// <summary>Null when App Insights is storing telemetry; otherwise why it is not.</summary>
    public Task<string?> GetProblem() => _problem.Value;

    private static async Task<string?> FindProblem(AppInsightsHelper appInsightsHelper)
    {
        DateTimeOffset? latestRequestTime;
        try
        {
            latestRequestTime = await appInsightsHelper.GetLatestRequestTime();
        }
        catch (Exception exception)
        {
            return $"App Insights could not be queried: {exception.Message}";
        }

        if (latestRequestTime > DateTimeOffset.UtcNow - MaxRequestAge)
            return null;

        var latest = latestRequestTime?.ToString("u") ?? "none in the last day";
        return $"App Insights has stored no Atlas requests for {MaxRequestAge.TotalMinutes} minutes (latest: {latest}). " +
               "The daily cap has probably been reached, so tests that read App Insights are skipped.";
    }
}
