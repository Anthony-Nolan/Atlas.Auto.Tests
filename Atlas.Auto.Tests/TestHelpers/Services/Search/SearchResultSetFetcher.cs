using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.SearchResults;

namespace Atlas.Auto.Tests.TestHelpers.Services.Search;

internal interface ISearchResultSetFetcher
{
    Task<DebugResponse<OriginalSearchResultSet>> Fetch(DebugSearchResultsRequest request);
}

internal class SearchResultSetFetcher : ISearchResultSetFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly ITopLevelFunctionsClient topLevelClient;

    public SearchResultSetFetcher(
        IDebugRequester debugRequester, 
        ITopLevelFunctionsClient topLevelClient)
    {
        this.debugRequester = debugRequester;
        this.topLevelClient = topLevelClient;
    }

    public async Task<DebugResponse<OriginalSearchResultSet>> Fetch(DebugSearchResultsRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            5, 10, async () => await FetchSearchResultSet(request));
    }

    private async Task<DebugResponse<OriginalSearchResultSet>> FetchSearchResultSet(DebugSearchResultsRequest request)
    {
        var resultSet = await topLevelClient.FetchSearchResultSet(request);
        return new DebugResponse<OriginalSearchResultSet>(resultSet);
    }
}