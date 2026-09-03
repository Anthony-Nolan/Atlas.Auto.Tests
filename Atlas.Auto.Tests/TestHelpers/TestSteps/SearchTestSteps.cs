using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using LochNessBuilder;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.Validation;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class SearchTestSteps : SearchTestStepsBase
{
    private readonly IPublicApiFunctionsClient _publicApiClient;
    private readonly IMatchingAlgorithmFunctionsClient _matchingClient;
    private readonly ITopLevelFunctionsClient _topLevelClient;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;
    private readonly NotificationFetcher<MatchingResultsNotification> _matchingNotificationFetcher;
    private readonly NotificationFetcher<SearchResultsNotification> _searchNotificationFetcher;

    public SearchTestSteps(
        IPublicApiFunctionsClient publicApiClient,
        IMatchingAlgorithmFunctionsClient matchingClient,
        ITopLevelFunctionsClient topLevelClient,
        PollyRetry pollyRetry,
        RetrySettings retry,
        DonorImportStepsForSearchTests donorImportSteps,
        ITestLogger logger,
        string testName)
        : base(donorImportSteps, logger, testName)
    {
        _publicApiClient = publicApiClient;
        _matchingClient = matchingClient;
        _topLevelClient = topLevelClient;
        _pollyRetry = pollyRetry;
        _retry = retry;
        _matchingNotificationFetcher = new NotificationFetcher<MatchingResultsNotification>(
            req => matchingClient.PeekMatchingResultNotifications(req), pollyRetry, retry.CheckDonorsInMatching, "Fetch matching notification");
        _searchNotificationFetcher = new NotificationFetcher<SearchResultsNotification>(
            req => topLevelClient.PeekSearchResultNotifications(req), pollyRetry, retry.FetchMessages, "Fetch search notification");
    }

    public async Task<string> CreateDonor(ImportDonorType donorType, Builder<ImportedHla> hlaBuilder)
    {
        return await _donorImportSteps.CreateDonor(donorType, hlaBuilder);
    }

    public async Task<SearchInitiationResponse> SubmitSearchRequest(string searchRequestFileName, bool? parallelMatchPrediction = null)
    {
        var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);
        searchRequest.ParallelMatchPrediction = parallelMatchPrediction;

        var response = await _pollyRetry.ExecuteWithRetry(
            async () => await _publicApiClient.PostSearchRequest(searchRequest),
            _retry.ApiCall, $"Submit search request '{searchRequestFileName}'");
        var result = AssertNotNull(response, "search API should have responded");

        result.WasSuccess.Should().BeTrue(
            "search request should have been accepted but got validation failures: {0}",
            string.Join(", ", result.ValidationFailures?.Select(f => f.ErrorMessage) ?? Array.Empty<string>()));

        _logger.LogInfo($"Search request id: {result.ResponseOnSuccess!.SearchIdentifier}");
        return result.ResponseOnSuccess;
    }

    public async Task<IEnumerable<RequestValidationFailure>> SubmitInvalidSearchRequest(string searchRequestFileName)
    {
        var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);

        var response = await _pollyRetry.ExecuteWithRetry(
            async () => await _publicApiClient.PostSearchRequest(searchRequest),
            _retry.ApiCall, $"Submit invalid search request '{searchRequestFileName}'");
        var result = AssertNotNull(response, "search API should have responded");

        result.WasSuccess.Should().BeFalse(
            "search request should have been rejected with validation failures but was accepted");

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
        _logger.LogStart(action);
        await CheckMatchingReturnsExpectedDonors(searchRequestId, new[] { expectedDonorCode });
        _logger.LogCompletion(action);
    }

    public async Task MatchingShouldOnlyReturnExpectedDonors(
        string searchRequestId, DonorChanges donorChanges)
    {
        const string action = "Check matching only returns expected donors and not non-matching donors";
        _logger.LogStart(action);

        var results = await CheckMatchingReturnsExpectedDonors(searchRequestId, donorChanges.NewlyMatching);

        results
            .Where(r => donorChanges.NoLongerMatching.Contains(r.DonorCode))
            .Should().BeEmpty("non-matching donors should not be returned in results");

        _logger.LogCompletion(action);
    }

    public async Task SearchShouldReturnExpectedDonor(string searchRequestId, string expectedDonorCode)
    {
        const string action = "Check search returns expected donor";
        _logger.LogStart(action);

        var notification = await FetchSearchResultsNotification(searchRequestId);
        notification.SearchShouldHaveBeenSuccessful();

        var searchResultSet = await _pollyRetry.ExecuteWithRetry(
            async () => await _topLevelClient.FetchSearchResultSet(notification.ToDebugSearchResultsRequest()),
            _retry.FetchResultSet, $"Fetch search result set for request '{searchRequestId}'");
        AssertNotNull(searchResultSet,
            $"search result set should have been fetched for request {searchRequestId}");

        var donorResult = searchResultSet!.GetDonorResult(expectedDonorCode);
        await DonorResultShouldBeAsExpected(donorResult, "SearchResult");

        _logger.LogCompletion(action);
    }

    private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string searchRequestId)
    {
        var notification = await _matchingNotificationFetcher.FetchNotification(
            m => m.SearchRequestId == searchRequestId,
            $"search request '{searchRequestId}'");
        return AssertNotNull(notification,
            $"matching notification should have been received for search request {searchRequestId}");
    }

    private async Task<SearchResultsNotification> FetchSearchResultsNotification(string searchRequestId)
    {
        var notification = await _searchNotificationFetcher.FetchNotification(
            m => m.SearchRequestId == searchRequestId,
            $"search request '{searchRequestId}'");
        return AssertNotNull(notification,
            $"search notification should have been received for search request {searchRequestId}");
    }

    private async Task<IEnumerable<MatchingAlgorithmResult>> CheckMatchingReturnsExpectedDonors(
        string searchRequestId,
        IEnumerable<string> expectedDonorCodes)
    {
        var notification = await FetchMatchingResultsNotification(searchRequestId);
        notification.MatchingShouldHaveBeenSuccessful();

        var matchingResultSet = await _pollyRetry.ExecuteWithRetry(
            async () => await _matchingClient.FetchMatchingResultSet(notification.ToDebugSearchResultsRequest()),
            _retry.FetchResultSet, $"Fetch matching result set for request '{searchRequestId}'");
        AssertNotNull(matchingResultSet,
            $"matching result set should have been fetched for search request {searchRequestId}");

        foreach (var expectedDonorCode in expectedDonorCodes)
        {
            var donorResult = matchingResultSet!.GetDonorResult(expectedDonorCode);
            await DonorResultShouldBeAsExpected(donorResult, "MatchingResult");
        }

        return matchingResultSet!.Results;
    }
}
