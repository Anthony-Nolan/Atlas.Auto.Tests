using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.SearchResults;

namespace Atlas.Auto.Tests.TestHelpers.Services.RepeatSearch;

internal interface IRepeatSearchResultSetFetcher
{
    Task<DebugResponse<RepeatSearchResultSet>> Fetch(DebugSearchResultsRequest request);
}

internal class RepeatSearchResultSetFetcher : IRepeatSearchResultSetFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly ITopLevelFunctionsClient topLevelClient;

    public RepeatSearchResultSetFetcher(
        IDebugRequester debugRequester, 
        ITopLevelFunctionsClient topLevelClient)
    {
        this.debugRequester = debugRequester;
        this.topLevelClient = topLevelClient;
    }

    public async Task<DebugResponse<RepeatSearchResultSet>> Fetch(DebugSearchResultsRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            5, 10, async () => await FetchSearchResultSet(request));
    }

    private async Task<DebugResponse<RepeatSearchResultSet>> FetchSearchResultSet(DebugSearchResultsRequest request)
    {
        var resultSet = await topLevelClient.FetchRepeatSearchResultSet(request);
        return new DebugResponse<RepeatSearchResultSet>(resultSet);
    }
}