using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using LochNessBuilder;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.DonorImport.FileSchema.Models;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class SearchTestSteps : SearchTestStepsBase
{
    private readonly PublicApiClient _publicApiClient;
    private readonly BlobStorageHelper _blobHelper;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;
    private readonly NotificationFetcher<MatchingResultsNotification> _matchingNotificationFetcher;
    private readonly NotificationFetcher<SearchResultsNotification> _searchNotificationFetcher;

    public SearchTestSteps(
        IServiceProvider provider,
        DonorImportStepsForSearchTests donorImportSteps,
        ILogger logger,
        string testName)
        : base(donorImportSteps, logger, testName)
    {
        _publicApiClient = provider.GetRequiredService<PublicApiClient>();
        _blobHelper = provider.GetRequiredService<BlobStorageHelper>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
        var sbClient = provider.GetRequiredService<ServiceBusClient>();
        var sbSettings = provider.GetRequiredService<ServiceBusSettings>();
        _matchingNotificationFetcher = new NotificationFetcher<MatchingResultsNotification>(
            sbClient, sbSettings.MatchingResultsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch matching notification");
        _searchNotificationFetcher = new NotificationFetcher<SearchResultsNotification>(
            sbClient, sbSettings.SearchResultsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch search notification");
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
            string.Join(", ", result.ValidationFailures.Select(f => f.ErrorMessage)));

        _logger.LogInformation($"Search request id: {result.Response!.SearchIdentifier}");
        return result.Response;
    }

    public async Task<IEnumerable<ValidationFailureResponse>> SubmitInvalidSearchRequest(string searchRequestFileName)
    {
        var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);

        var response = await _pollyRetry.ExecuteWithRetry(
            async () => await _publicApiClient.PostSearchRequest(searchRequest),
            _retry.ApiCall, $"Submit invalid search request '{searchRequestFileName}'");
        var result = AssertNotNull(response, "Search API should have responded");

        result.WasSuccess.Should().BeFalse(
            "Search request should have been rejected with validation failures but was accepted");

        return result.ValidationFailures;
    }

    public async Task MatchingShouldFailHlaValidation(string searchRequestId)
    {
        var notification = await FetchMatchingResultsNotification(searchRequestId);
        notification.MatchingShouldHaveFailedHlaValidation();
    }

    public async Task MatchingShouldReturnExpectedDonor(string searchRequestId, string expectedDonorCode)
    {
        await CheckMatchingReturnsExpectedDonors(searchRequestId, new[] { expectedDonorCode });
    }

    public async Task MatchingShouldOnlyReturnExpectedDonors(
        string searchRequestId, DonorChanges donorChanges)
    {
        var results = await CheckMatchingReturnsExpectedDonors(searchRequestId, donorChanges.NewlyMatching);

        results
            .Where(r => donorChanges.NoLongerMatching.Contains(r.DonorCode))
            .Should().BeEmpty("Non-matching donors should not be returned in results");
    }

    public async Task SearchShouldReturnExpectedDonor(string searchRequestId, string expectedDonorCode)
    {
        var notification = await FetchSearchResultsNotification(searchRequestId);
        notification.SearchShouldHaveBeenSuccessful();

        var searchResultSet = await _pollyRetry.ExecuteWithRetry(
            async () => await _blobHelper.DownloadResultSet<OriginalSearchResultSet, SearchResult>(
                notification.BlobStorageContainerName, notification.ResultsFileName, notification.BatchFolderName),
            _retry.FetchResultSet, $"Fetch search result set for request '{searchRequestId}'");
        AssertNotNull(searchResultSet,
            $"Search result set should have been fetched for request {searchRequestId}");

        var donorResult = searchResultSet!.GetDonorResult(expectedDonorCode);
        await DonorResultShouldBeAsExpected(donorResult, "SearchResult");
    }

    private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string searchRequestId)
    {
        var notification = await _matchingNotificationFetcher.FetchNotification(
            m => m.SearchRequestId == searchRequestId,
            $"Search request '{searchRequestId}'");
        return AssertNotNull(notification,
            $"Matching notification should have been received for search request {searchRequestId}");
    }

    private async Task<SearchResultsNotification> FetchSearchResultsNotification(string searchRequestId)
    {
        var notification = await _searchNotificationFetcher.FetchNotification(
            m => m.SearchRequestId == searchRequestId,
            $"Search request '{searchRequestId}'");
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
            async () => await _blobHelper.DownloadResultSet<OriginalMatchingAlgorithmResultSet, MatchingAlgorithmResult>(
                notification.BlobStorageContainerName, notification.ResultsFileName, notification.BatchFolderName),
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
