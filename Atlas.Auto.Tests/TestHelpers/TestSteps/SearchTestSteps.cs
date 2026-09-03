using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.Validation;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class SearchTestSteps : SearchTestStepsBase
{
    private readonly Func<SearchRequest, Task<ResponseFromValidatedRequest<SearchInitiationResponse>>> _postSearchRequest;
    private readonly NotificationFetcher<MatchingResultsNotification> _matchingNotificationFetcher;
    private readonly Func<DebugSearchResultsRequest, Task<OriginalMatchingAlgorithmResultSet>> _fetchMatchingResultSet;
    private readonly NotificationFetcher<SearchResultsNotification> _searchNotificationFetcher;
    private readonly Func<DebugSearchResultsRequest, Task<OriginalSearchResultSet>> _fetchSearchResultSet;

    public SearchTestSteps(
        Func<SearchRequest, Task<ResponseFromValidatedRequest<SearchInitiationResponse>>> postSearchRequest,
        NotificationFetcher<MatchingResultsNotification> matchingNotificationFetcher,
        Func<DebugSearchResultsRequest, Task<OriginalMatchingAlgorithmResultSet>> fetchMatchingResultSet,
        NotificationFetcher<SearchResultsNotification> searchNotificationFetcher,
        Func<DebugSearchResultsRequest, Task<OriginalSearchResultSet>> fetchSearchResultSet,
        IDonorImportStepsForSearchTests donorImportSteps,
        ITestLogger logger,
        string testName)
        : base(donorImportSteps, logger, testName)
    {
        _postSearchRequest = postSearchRequest;
        _matchingNotificationFetcher = matchingNotificationFetcher;
        _fetchMatchingResultSet = fetchMatchingResultSet;
        _searchNotificationFetcher = searchNotificationFetcher;
        _fetchSearchResultSet = fetchSearchResultSet;
    }

    public async Task<string> CreateDonor(ImportDonorType donorType)
    {
        return await donorImportSteps.CreateDonorWithSearchTestPhenotype(donorType);
    }

    public async Task<string> CreateDonorWithNew(ImportDonorType donorType)
    {
        return await donorImportSteps.CreateDonorWithNewDnaPhenotype(donorType);
    }

    public async Task<string> CreateDonorWithAssociatedAntigen(ImportDonorType donorType)
    {
        return await donorImportSteps.CreateDonorWithAssociatedAntigenPhenotype(donorType);
    }

    public async Task<SearchInitiationResponse> SubmitSearchRequest(string searchRequestFileName, bool? parallelMatchPrediction = null)
    {
        var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);
        searchRequest.ParallelMatchPrediction = parallelMatchPrediction;

        var response = await PollyRetry.ExecuteWithRetry(
            async () => await _postSearchRequest(searchRequest), 5, 5, "Submit search request");
        var result = AssertNotNull(response, "Search API should have responded", "Submit search request");

        logger.AssertThenLogAndThrow(
            () => result.WasSuccess.Should().BeTrue(
                "Search request should have been accepted but got validation failures: {0}",
                string.Join(", ", result.ValidationFailures?.Select(f => f.ErrorMessage) ?? Array.Empty<string>())),
            "Validate search response is successful");

        logger.LogInfo($"Search request id: {result.ResponseOnSuccess!.SearchIdentifier}");
        return result.ResponseOnSuccess;
    }

    public async Task<IEnumerable<RequestValidationFailure>> SubmitInvalidSearchRequest(string searchRequestFileName)
    {
        var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);

        var response = await PollyRetry.ExecuteWithRetry(
            async () => await _postSearchRequest(searchRequest), 5, 5, "Submit search request");
        var result = AssertNotNull(response, "Search API should have responded", "Submit invalid search request");

        logger.AssertThenLogAndThrow(
            () => result.WasSuccess.Should().BeFalse(
                "Search request should have been rejected with validation failures but was accepted"),
            "Validate search response is a validation failure");

        return result.ValidationFailures!;
    }

    public async Task MatchingShouldFailHlaValidation(string searchRequestId)
    {
        var notification = await FetchMatchingResultsNotification(searchRequestId);
        notification.MatchingShouldHaveFailedHlaValidation();
    }

    public async Task MatchingShouldReturnExpectedDonor(string searchRequestId, string expectedDonorCode)
    {
        const string action = "Check matching returns expected donor";
        logger.LogStart(action);
        await CheckMatchingReturnsExpectedDonors(searchRequestId, new[] { expectedDonorCode });
        logger.LogCompletion(action);
    }

    public async Task MatchingShouldOnlyReturnExpectedDonors(
        string searchRequestId, DonorChanges donorChanges)
    {
        const string action = "Check matching only returns expected donors and not non-matching donors";
        logger.LogStart(action);

        var results = await CheckMatchingReturnsExpectedDonors(searchRequestId, donorChanges.NewlyMatching);

        logger.AssertThenLogAndThrow(() => results
                .Where(r => donorChanges.NoLongerMatching.Contains(r.DonorCode))
                .Should().BeEmpty(),
            "Check non-matching donors were not returned");

        logger.LogCompletion(action);
    }

    public async Task SearchShouldReturnExpectedDonor(string searchRequestId, string expectedDonorCode)
    {
        const string action = "Check search returns expected donor";
        logger.LogStart(action);

        var notification = await FetchSearchResultsNotification(searchRequestId);
        notification.SearchShouldHaveBeenSuccessful();

        var searchResultSet = await PollyRetry.ExecuteWithRetry(
            async () => await _fetchSearchResultSet(notification.ToDebugSearchResultsRequest()), 5, 10, "Fetch search result set");
        AssertNotNull(searchResultSet,
            $"Search result set should have been fetched for request {searchRequestId}",
            "Fetch search result set");

        var donorResult = searchResultSet!.GetDonorResult(expectedDonorCode);
        await DonorResultShouldBeAsExpected(donorResult, "SearchResult");

        logger.LogCompletion(action);
    }

    private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string searchRequestId)
    {
        var notification = await _matchingNotificationFetcher.FetchNotification(
            m => m.SearchRequestId == searchRequestId);
        return AssertNotNull(notification,
            $"Matching notification should have been received for search request {searchRequestId}",
            "Fetch matching results notification");
    }

    private async Task<SearchResultsNotification> FetchSearchResultsNotification(string searchRequestId)
    {
        var notification = await _searchNotificationFetcher.FetchNotification(
            m => m.SearchRequestId == searchRequestId);
        return AssertNotNull(notification,
            $"Search notification should have been received for search request {searchRequestId}",
            "Fetch search results notification");
    }

    private async Task<IEnumerable<MatchingAlgorithmResult>> CheckMatchingReturnsExpectedDonors(
        string searchRequestId,
        IEnumerable<string> expectedDonorCodes)
    {
        var notification = await FetchMatchingResultsNotification(searchRequestId);
        notification.MatchingShouldHaveBeenSuccessful();

        var matchingResultSet = await PollyRetry.ExecuteWithRetry(
            async () => await _fetchMatchingResultSet(notification.ToDebugSearchResultsRequest()), 5, 10, "Fetch matching result set");
        AssertNotNull(matchingResultSet,
            $"Matching result set should have been fetched for search request {searchRequestId}",
            "Fetch matching result set");

        foreach (var expectedDonorCode in expectedDonorCodes)
        {
            var donorResult = matchingResultSet!.GetDonorResult(expectedDonorCode);
            await DonorResultShouldBeAsExpected(donorResult, "MatchingResult");
        }

        return matchingResultSet!.Results;
    }
}
