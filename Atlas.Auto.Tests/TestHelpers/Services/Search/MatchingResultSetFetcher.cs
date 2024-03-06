using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.SearchResults;

namespace Atlas.Auto.Tests.TestHelpers.Services.Search;

internal interface IMatchingResultSetFetcher
{
    Task<DebugResponse<OriginalMatchingAlgorithmResultSet>> Fetch(DebugSearchResultsRequest request);
}

internal class MatchingResultSetFetcher : IMatchingResultSetFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly IMatchingAlgorithmFunctionsClient matchingClient;

    public MatchingResultSetFetcher(
        IDebugRequester debugRequester, 
        IMatchingAlgorithmFunctionsClient matchingClient)
    {
        this.debugRequester = debugRequester;
        this.matchingClient = matchingClient;
    }

    public async Task<DebugResponse<OriginalMatchingAlgorithmResultSet>> Fetch(DebugSearchResultsRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            5, 10, async () => await FetchMatchingResultSet(request));
    }

    private async Task<DebugResponse<OriginalMatchingAlgorithmResultSet>> FetchMatchingResultSet(DebugSearchResultsRequest request)
    {
        var resultSet = await matchingClient.FetchMatchingResultSet(request);
        return new DebugResponse<OriginalMatchingAlgorithmResultSet>(resultSet);
    }
}