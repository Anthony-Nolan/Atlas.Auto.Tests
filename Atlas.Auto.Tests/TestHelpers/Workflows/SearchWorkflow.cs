﻿using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services.Search;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

/// <summary>
/// Workflow to execute a search request and determine the outcome.
/// </summary>
internal interface ISearchWorkflow
{
    Task<DebugResponse<SearchInitiationResponse>> SubmitSearchRequest(SearchRequest request);
    Task<DebugResponse<MatchingResultsNotification>> FetchMatchingResultsNotification(string searchRequestId);
    Task<DebugResponse<OriginalMatchingAlgorithmResultSet>> FetchMatchingResultSet(string resultsFileName, string? batchFolderName);
    Task<DebugResponse<SearchResultsNotification>> FetchSearchResultsNotification(string searchRequestId);
    Task<DebugResponse<OriginalSearchResultSet>> FetchSearchResultSet(string resultsFileName, string? batchFolderName);
}

internal class SearchWorkflow : ISearchWorkflow
{
    private readonly ISearchRequester searchRequester;
    private readonly IMatchingNotificationFetcher matchingNotificationFetcher;
    private readonly IMatchingResultSetFetcher matchingResultSetFetcher;
    private readonly ISearchNotificationFetcher searchNotificationFetcher;
    private readonly ISearchResultSetFetcher searchResultSetFetcher;

    public SearchWorkflow(
        ISearchRequester searchRequester,
        IMatchingNotificationFetcher matchingNotificationFetcher,
        IMatchingResultSetFetcher matchingResultSetFetcher,
        ISearchNotificationFetcher searchNotificationFetcher,
        ISearchResultSetFetcher searchResultSetFetcher)
    {
        this.searchRequester = searchRequester;
        this.matchingNotificationFetcher = matchingNotificationFetcher;
        this.matchingResultSetFetcher = matchingResultSetFetcher;
        this.searchNotificationFetcher = searchNotificationFetcher;
        this.searchResultSetFetcher = searchResultSetFetcher;
    }

    public async Task<DebugResponse<SearchInitiationResponse>> SubmitSearchRequest(SearchRequest request)
    {
        return await searchRequester.SubmitValidSearchRequest(request);
    }

    public async Task<DebugResponse<MatchingResultsNotification>> FetchMatchingResultsNotification(string searchRequestId)
    {
        return await matchingNotificationFetcher.FetchNotification(searchRequestId);
    }

    public async Task<DebugResponse<OriginalMatchingAlgorithmResultSet>> FetchMatchingResultSet(string resultsFileName, string? batchFolderName)
    {
        return await matchingResultSetFetcher.Fetch(resultsFileName, batchFolderName);
    }

    public async Task<DebugResponse<SearchResultsNotification>> FetchSearchResultsNotification(string searchRequestId)
    {
        return await searchNotificationFetcher.FetchNotification(searchRequestId);
    }

    public async Task<DebugResponse<OriginalSearchResultSet>> FetchSearchResultSet(string resultsFileName, string? batchFolderName)
    {
        return await searchResultSetFetcher.Fetch(resultsFileName, batchFolderName);
    }
}