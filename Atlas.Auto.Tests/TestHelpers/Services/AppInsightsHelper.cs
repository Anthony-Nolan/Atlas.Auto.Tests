using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class AppInsightsHelper(LogsQueryClient logsClient, AppInsightsSettings settings)
{
    private const string HlaExpansionFailuresQuery = """
        AppEvents
        | where Name startswith "HLA Expansion"
        | extend
            DonorInfo = parse_json(tostring(Properties["DonorInfo"])),
            Locus = tostring(Properties["Locus"]),
            HlaName = tostring(Properties["HlaName"])
        | distinct
            InvalidHLA = strcat(Locus, HlaName),
            ExternalDonorCode = tostring(DonorInfo["ExternalDonorCode"]),
            ExceptionType = tostring(Properties["InnerExceptionType"])
        | summarize
            ExceptionType = make_list(ExceptionType, 10)[0],
            ExternalDonorCodes = make_list(ExternalDonorCode, 1000),
            DonorCount = count() by InvalidHLA
        | order by DonorCount desc
        """;

    private const string PrecomputedGenotypeSetUsageQuery = """
        AppEvents
        | where Name == "Precomputed genotype sets used"
        | where tostring(Properties["SearchRequestId"]) == searchRequestId
        | summarize
            EventCount = count(),
            PrecomputeModes = strcat_array(make_set(tostring(Properties["PrecomputeMode"])), ","),
            UsePrecomputedGenotypeSets = strcat_array(make_set(tostring(Properties["UsePrecomputedGenotypeSets"])), ","),
            DonorCount = tolong(sum(todouble(Measurements["DonorCount"]))),
            PrecomputedCount = tolong(sum(todouble(Measurements["PrecomputedCount"])))
        """;

    /// <summary>
    /// Null until App Insights has ingested at least one usage event of the search, so it can be retried.
    /// </summary>
    public async Task<PrecomputedGenotypeSetUsage?> GetPrecomputedGenotypeSetUsage(string searchRequestId, int daysToQuery = 1)
    {
        // CreateQuery quotes and escapes the id
        var query = LogsQueryClient.CreateQuery($"let searchRequestId = {searchRequestId};") + "\n" + PrecomputedGenotypeSetUsageQuery;
        var response = await logsClient.QueryWorkspaceAsync(
            settings.WorkspaceId,
            query,
            new QueryTimeRange(TimeSpan.FromDays(daysToQuery)));

        var row = response.Value.Table.Rows.SingleOrDefault();
        var eventCount = (long)(row?[nameof(PrecomputedGenotypeSetUsage.EventCount)] ?? 0L);
        if (row == null || eventCount == 0)
            return null;

        return new PrecomputedGenotypeSetUsage(
            eventCount,
            row[nameof(PrecomputedGenotypeSetUsage.PrecomputeModes)]?.ToString() ?? string.Empty,
            row[nameof(PrecomputedGenotypeSetUsage.UsePrecomputedGenotypeSets)]?.ToString() ?? string.Empty,
            (long)(row[nameof(PrecomputedGenotypeSetUsage.DonorCount)] ?? 0L),
            (long)(row[nameof(PrecomputedGenotypeSetUsage.PrecomputedCount)] ?? 0L));
    }

    /// <summary>
    /// The time of the latest request any Atlas app has sent to App Insights, or null when there is none in the last day.
    /// The function apps have timer triggers, so on a working environment there is always a recent one.
    /// </summary>
    public async Task<DateTimeOffset?> GetLatestRequestTime()
    {
        var response = await logsClient.QueryWorkspaceAsync(
            settings.WorkspaceId,
            "AppRequests | summarize LatestRequestTime = max(TimeGenerated)",
            new QueryTimeRange(TimeSpan.FromDays(1)));

        return response.Value.Table.Rows.SingleOrDefault()?.GetDateTimeOffset("LatestRequestTime");
    }

    public async Task<List<HlaExpansionFailure>> GetHlaExpansionFailures(int daysToQuery = 14)
    {
        var response = await logsClient.QueryWorkspaceAsync(
            settings.WorkspaceId,
            HlaExpansionFailuresQuery,
            new QueryTimeRange(TimeSpan.FromDays(daysToQuery)));

        return response.Value.Table.Rows
            .Select(MapRow)
            .ToList();
    }

    private static HlaExpansionFailure MapRow(LogsTableRow row) => new()
    {
        InvalidHLA = row[nameof(HlaExpansionFailure.InvalidHLA)]?.ToString() ?? string.Empty,
        ExceptionType = row[nameof(HlaExpansionFailure.ExceptionType)]?.ToString() ?? string.Empty,
        ExternalDonorCodes = JsonConvert.DeserializeObject<string[]>(
            row[nameof(HlaExpansionFailure.ExternalDonorCodes)]?.ToString() ?? "[]") ?? [],
        DonorCount = (long)(row[nameof(HlaExpansionFailure.DonorCount)] ?? 0L)
    };
}
