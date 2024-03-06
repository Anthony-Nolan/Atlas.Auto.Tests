using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.SearchResults;

namespace Atlas.Auto.Tests.TestHelpers.Services.RepeatSearch;

internal interface IRepeatMatchingResultSetFetcher
{
    Task<DebugResponse<RepeatMatchingAlgorithmResultSet>> Fetch(DebugSearchResultsRequest request);
}

internal class RepeatMatchingResultSetFetcher : IRepeatMatchingResultSetFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly IRepeatSearchFunctionsClient repeatSearchClient;

    public RepeatMatchingResultSetFetcher(
        IDebugRequester debugRequester,
        IRepeatSearchFunctionsClient repeatSearchClient)
    {
        this.debugRequester = debugRequester;
        this.repeatSearchClient = repeatSearchClient;
    }

    public async Task<DebugResponse<RepeatMatchingAlgorithmResultSet>> Fetch(DebugSearchResultsRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            5, 10, async () => await FetchMatchingResultSet(request));
    }

    private async Task<DebugResponse<RepeatMatchingAlgorithmResultSet>> FetchMatchingResultSet(DebugSearchResultsRequest request)
    {
        var resultSet = await repeatSearchClient.FetchMatchingResultSet(request);
        return new DebugResponse<RepeatMatchingAlgorithmResultSet>(resultSet);
    }
}