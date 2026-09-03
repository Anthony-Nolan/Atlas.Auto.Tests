using Atlas.Auto.Tests.TestHelpers.Assertions;
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
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class RepeatSearchTestSteps : SearchTestStepsBase
{
    private readonly IPublicApiFunctionsClient _publicApiClient;
    private readonly IRepeatSearchFunctionsClient _repeatSearchClient;
    private readonly ITopLevelFunctionsClient _topLevelClient;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;
    private readonly NotificationFetcher<MatchingResultsNotification> _matchingNotificationFetcher;
    private readonly NotificationFetcher<SearchResultsNotification> _searchNotificationFetcher;
    private readonly SearchTestSteps _searchTestSteps;

    public RepeatSearchTestSteps(
        IServiceProvider provider,
        SearchTestSteps searchTestSteps,
        DonorImportStepsForSearchTests donorImportSteps,
        ITestLogger logger,
        string testName)
        : base(donorImportSteps, logger, testName)
    {
        _publicApiClient = provider.GetRequiredService<IPublicApiFunctionsClient>();
        _repeatSearchClient = provider.GetRequiredService<IRepeatSearchFunctionsClient>();
        _topLevelClient = provider.GetRequiredService<ITopLevelFunctionsClient>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
        _searchTestSteps = searchTestSteps;
        _matchingNotificationFetcher = new NotificationFetcher<MatchingResultsNotification>(
            req => _repeatSearchClient.PeekMatchingResultNotifications(req), _pollyRetry, _retry.FetchMessages, "Fetch repeat matching notification");
        _searchNotificationFetcher = new NotificationFetcher<SearchResultsNotification>(
            req => _topLevelClient.PeekRepeatSearchResultNotifications(req), _pollyRetry, _retry.FetchMessages, "Fetch repeat search notification");
    }

    public async Task<string> CreateDonor(ImportDonorType donorType, Builder<ImportedHla> hlaBuilder)
    {
        return await _donorImportSteps.CreateDonor(donorType, hlaBuilder);
    }

    public async Task EditDonorHla(string donorCode, ImportDonorType donorType, Builder<ImportedHla> hlaBuilder)
    {
        await _donorImportSteps.EditDonorHla(donorCode, donorType, hlaBuilder);
    }

    public async Task DeleteDonors(IReadOnlyCollection<string> donorCodes)
    {
        await _donorImportSteps.DeleteDonors(donorCodes);
    }

    public async Task<string> OriginalSearchShouldOnlyReturnExpectedDonors(
        string searchRequestFileName, DonorChanges donorChanges, bool? parallelMatchPrediction = null)
    {
        var response = await _searchTestSteps.SubmitSearchRequest(searchRequestFileName, parallelMatchPrediction);
        await _searchTestSteps.MatchingShouldOnlyReturnExpectedDonors(response.SearchIdentifier, donorChanges);
        return response.SearchIdentifier;
    }

    public async Task<string> SubmitRepeatSearchRequest(
        string searchRequestFileName,
        string originalSearchId,
        DateTimeOffset searchCutOff,
        bool? parallelMatchPrediction = null)
    {
        var originalSearchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);
        originalSearchRequest.ParallelMatchPrediction = parallelMatchPrediction;

        var repeatRequest = new RepeatSearchRequest
        {
            OriginalSearchId = originalSearchId,
            SearchRequest = originalSearchRequest,
            SearchCutoffDate = searchCutOff
        };

        var response = await _pollyRetry.ExecuteWithRetry(
            async () => await _publicApiClient.PostRepeatSearchRequest(repeatRequest),
            _retry.ApiCall, $"Submit repeat search request for original search '{originalSearchId}'");
        var result = AssertNotNull(response, "repeat search API should have responded");

        result.WasSuccess.Should().BeTrue(
            "repeat search request should have been accepted but got validation failures: {0}",
            string.Join(", ", result.ValidationFailures?.Select(f => f.ErrorMessage) ?? Array.Empty<string>()));

        return result.ResponseOnSuccess!.RepeatSearchIdentifier;
    }

    public async Task RepeatMatchingShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId,
        string searchId,
        DonorChanges donorChanges)
    {
        const string action = "Check repeat matching identifies expected changes";
        _logger.LogStart(action);

        var notification = await FetchMatchingResultsNotification(repeatSearchId, searchId);
        notification.MatchingShouldHaveBeenSuccessful();

        var matchingResultSet = await _pollyRetry.ExecuteWithRetry(
            async () => await _repeatSearchClient.FetchMatchingResultSet(notification.ToDebugSearchResultsRequest()),
            _retry.FetchResultSet, $"Fetch repeat matching result set for repeat search '{repeatSearchId}'");
        AssertNotNull(matchingResultSet,
            "matching result set should have been fetched for repeat search");

        ExpectedDonorsShouldNoLongerMatch(matchingResultSet!.NoLongerMatchingDonors, donorChanges.NoLongerMatching);

        foreach (var newlyMatchedDonor in donorChanges.NewlyMatching)
        {
            var donorResult = matchingResultSet.GetDonorResult(newlyMatchedDonor);
            await DonorResultShouldBeAsExpected(donorResult, "MatchingResult");
        }

        _logger.LogCompletion(action);
    }

    public async Task RepeatSearchShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId,
        string searchId,
        DonorChanges donorChanges)
    {
        const string action = "Check repeat search identifies expected changes";
        _logger.LogStart(action);

        var notification = await FetchSearchResultsNotification(repeatSearchId, searchId);
        notification.SearchShouldHaveBeenSuccessful();

        var searchResultSet = await _pollyRetry.ExecuteWithRetry(
            async () => await _topLevelClient.FetchRepeatSearchResultSet(notification.ToDebugSearchResultsRequest()),
            _retry.FetchResultSet, $"Fetch repeat search result set for repeat search '{repeatSearchId}'");
        AssertNotNull(searchResultSet,
            "search result set should have been fetched for repeat search");

        ExpectedDonorsShouldNoLongerMatch(searchResultSet!.NoLongerMatchingDonorCodes, donorChanges.NoLongerMatching);

        foreach (var newlyMatchedDonor in donorChanges.NewlyMatching)
        {
            var donorResult = searchResultSet.GetDonorResult(newlyMatchedDonor);
            await DonorResultShouldBeAsExpected(donorResult, "SearchResult");
        }

        _logger.LogCompletion(action);
    }

    public async Task RepeatRequestMissingRequiredInfoShouldReturnValidationErrors()
    {
        var response = await _pollyRetry.ExecuteWithRetry(
            async () => await _publicApiClient.PostRepeatSearchRequest(new RepeatSearchRequest()),
            _retry.ApiCall, "Submit invalid repeat search request (missing required fields)");
        var result = AssertNotNull(response, "repeat search API should have responded");

        result.WasSuccess.Should().BeFalse(
            "repeat search request should have been rejected with validation failures but was accepted");

        var validationErrors = result.ValidationFailures!.ToList();
        validationErrors.ShouldContain("'Original Search Id' must not be empty.");
        validationErrors.ShouldContain("'Search Cutoff Date' must not be empty.");
    }

    private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string repeatSearchId, string searchId)
    {
        var notification = await _matchingNotificationFetcher.FetchNotification(
            m => m.RepeatSearchRequestId == repeatSearchId && m.SearchRequestId == searchId,
            $"repeat search '{repeatSearchId}', original search '{searchId}'");
        return AssertNotNull(notification,
            $"matching notification should have been received for repeat search {repeatSearchId}");
    }

    private async Task<SearchResultsNotification> FetchSearchResultsNotification(string repeatSearchId, string searchId)
    {
        var notification = await _searchNotificationFetcher.FetchNotification(
            m => m.RepeatSearchRequestId == repeatSearchId && m.SearchRequestId == searchId,
            $"repeat search '{repeatSearchId}', original search '{searchId}'");
        return AssertNotNull(notification,
            $"search notification should have been received for repeat search {repeatSearchId}");
    }

    private static void ExpectedDonorsShouldNoLongerMatch(
        IEnumerable<string> noLongerMatchingDonors,
        IEnumerable<string> expectedDonorCodes)
    {
        noLongerMatchingDonors.Should().Contain(expectedDonorCodes,
            "expected donors should be in the no-longer-matching list");
    }
}
