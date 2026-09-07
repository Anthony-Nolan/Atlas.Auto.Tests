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
using Microsoft.Extensions.DependencyInjection;

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
        IServiceProvider provider,
        DonorImportStepsForSearchTests donorImportSteps,
        ITestLogger logger,
        string testName)
        : base(donorImportSteps, logger, testName)
    {
        _publicApiClient = provider.GetRequiredService<IPublicApiFunctionsClient>();
        _matchingClient = provider.GetRequiredService<IMatchingAlgorithmFunctionsClient>();
        _topLevelClient = provider.GetRequiredService<ITopLevelFunctionsClient>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
        _matchingNotificationFetcher = new NotificationFetcher<MatchingResultsNotification>(
            req => _matchingClient.PeekMatchingResultNotifications(req), _pollyRetry, _retry.FetchMessages, "Fetch matching notification");
        _searchNotificationFetcher = new NotificationFetcher<SearchResultsNotification>(
            req => _topLevelClient.PeekSearchResultNotifications(req), _pollyRetry, _retry.FetchMessages, "Fetch search notification");
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
        var result = AssertNotNull(response, "Search API should have responded");

        result.WasSuccess.Should().BeTrue(
            "Search request should have been accepted but got validation failures: {0}",
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
        var result = AssertNotNull(response, "Search API should have responded");

        result.WasSuccess.Should().BeFalse(
            "Search request should have been rejected with validation failures but was accepted");

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
            .Should().BeEmpty("Non-matching donors should not be returned in results");

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
            $"Search result set should have been fetched for request {searchRequestId}");

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
            $"Matching notification should have been received for search request {searchRequestId}");
    }

    private async Task<SearchResultsNotification> FetchSearchResultsNotification(string searchRequestId)
    {
        var notification = await _searchNotificationFetcher.FetchNotification(
            m => m.SearchRequestId == searchRequestId,
            $"search request '{searchRequestId}'");
        return AssertNotNull(notification,
            $"Search notification should have been received for search request {searchRequestId}");
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
            $"Matching result set should have been fetched for search request {searchRequestId}");

        foreach (var expectedDonorCode in expectedDonorCodes)
        {
            var donorResult = matchingResultSet!.GetDonorResult(expectedDonorCode);
            await DonorResultShouldBeAsExpected(donorResult, "MatchingResult");
        }

        return matchingResultSet!.Results;
    }
}
