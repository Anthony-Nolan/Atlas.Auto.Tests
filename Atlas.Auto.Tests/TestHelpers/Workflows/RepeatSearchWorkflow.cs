using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services.RepeatSearch;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.Validation;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

/// <summary>
/// Workflow to execute a repeat search request and determine the outcome.
/// </summary>
internal interface IRepeatSearchWorkflow
{
    Task<DebugResponse<SearchInitiationResponse>> SubmitRepeatSearchRequest(RepeatSearchRequest request);
    Task<DebugResponse<MatchingResultsNotification>> FetchMatchingResultsNotification(string repeatSearchRequestId, string searchRequestId);
    Task<DebugResponse<RepeatMatchingAlgorithmResultSet>> FetchMatchingResultSet(DebugSearchResultsRequest request);
    Task<DebugResponse<SearchResultsNotification>> FetchSearchResultsNotification(string repeatSearchRequestId, string searchRequestId);
    Task<DebugResponse<RepeatSearchResultSet>> FetchSearchResultSet(DebugSearchResultsRequest request);
    Task<DebugResponse<IEnumerable<RequestValidationFailure>>> SubmitInvalidRepeatSearchRequest(RepeatSearchRequest request);
}

internal class RepeatSearchWorkflow : IRepeatSearchWorkflow
{
    private readonly IRepeatSearchRequester repeatSearchRequester;
    private readonly IRepeatMatchingNotificationFetcher matchingNotificationFetcher;
    private readonly IRepeatMatchingResultSetFetcher matchingResultSetFetcher;
    private readonly IRepeatSearchNotificationFetcher searchNotificationFetcher;
    private readonly IRepeatSearchResultSetFetcher searchResultSetFetcher;

    public RepeatSearchWorkflow(
        IRepeatSearchRequester repeatSearchRequester,
        IRepeatMatchingNotificationFetcher matchingNotificationFetcher,
        IRepeatMatchingResultSetFetcher matchingResultSetFetcher,
        IRepeatSearchNotificationFetcher searchNotificationFetcher,
        IRepeatSearchResultSetFetcher searchResultSetFetcher)
    {
        this.repeatSearchRequester = repeatSearchRequester;
        this.matchingNotificationFetcher = matchingNotificationFetcher;
        this.matchingResultSetFetcher = matchingResultSetFetcher;
        this.searchNotificationFetcher = searchNotificationFetcher;
        this.searchResultSetFetcher = searchResultSetFetcher;
    }

    public async Task<DebugResponse<SearchInitiationResponse>> SubmitRepeatSearchRequest(RepeatSearchRequest request)
    {
        return await repeatSearchRequester.SubmitValidSearchRequest(request);
    }

    public async Task<DebugResponse<MatchingResultsNotification>> FetchMatchingResultsNotification(string repeatSearchRequestId, string searchRequestId)
    {
        return await matchingNotificationFetcher.FetchNotification(repeatSearchRequestId, searchRequestId);
    }

    public async Task<DebugResponse<RepeatMatchingAlgorithmResultSet>> FetchMatchingResultSet(DebugSearchResultsRequest request)
    {
        return await matchingResultSetFetcher.Fetch(request);
    }

    public async Task<DebugResponse<SearchResultsNotification>> FetchSearchResultsNotification(string repeatSearchRequestId, string searchRequestId)
    {
        return await searchNotificationFetcher.FetchNotification(repeatSearchRequestId, searchRequestId);
    }

    public async Task<DebugResponse<RepeatSearchResultSet>> FetchSearchResultSet(DebugSearchResultsRequest request)
    {
        return await searchResultSetFetcher.Fetch(request);
    }

    public async Task<DebugResponse<IEnumerable<RequestValidationFailure>>> SubmitInvalidRepeatSearchRequest(RepeatSearchRequest request)
    {
        return await repeatSearchRequester.SubmitInvalidSearchRequest(request);
    }
}