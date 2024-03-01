using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.SearchResults;

namespace Atlas.Auto.Tests.TestHelpers.Services.Search;

internal interface IMatchingResultSetFetcher
{
    Task<DebugResponse<OriginalMatchingAlgorithmResultSet>> Fetch(string resultsFileName, string? batchFolderName);
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

    public async Task<DebugResponse<OriginalMatchingAlgorithmResultSet>> Fetch(string resultsFileName, string? batchFolderName)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            5, 10, async () => await FetchMatchingResultSet(resultsFileName, batchFolderName));
    }

    private async Task<DebugResponse<OriginalMatchingAlgorithmResultSet>> FetchMatchingResultSet(string resultsFileName, string? batchFolderName)
    {
        var resultSet = await matchingClient.FetchMatchingResultSet(new DebugSearchResultsRequest
        {
            SearchResultFileName = resultsFileName,
            BatchFolderName = batchFolderName
        });

        return new DebugResponse<OriginalMatchingAlgorithmResultSet>(resultSet);
    }
}