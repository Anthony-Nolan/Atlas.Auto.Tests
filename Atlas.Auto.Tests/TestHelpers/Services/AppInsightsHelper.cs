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
