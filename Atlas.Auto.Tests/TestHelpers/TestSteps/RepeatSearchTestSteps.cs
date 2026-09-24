using Atlas.Auto.Tests.TestHelpers.Assertions;
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

internal class RepeatSearchTestSteps : SearchTestStepsBase
{
    private readonly NotificationFetcher<MatchingResultsNotification> _matchingNotificationFetcher;
    private readonly NotificationFetcher<SearchResultsNotification> _searchNotificationFetcher;
    private readonly SearchTestSteps _searchTestSteps;

    public RepeatSearchTestSteps(
        IServiceProvider provider,
        SearchTestSteps searchTestSteps,
        DonorImportStepsForSearchTests donorImportSteps,
        ILogger logger,
        string testName)
        : base(provider, donorImportSteps, logger, testName)
    {
        _searchTestSteps = searchTestSteps;
        var sbClient = provider.GetRequiredService<ServiceBusClient>();
        var sbSettings = provider.GetRequiredService<ServiceBusSettings>();
        _matchingNotificationFetcher = new NotificationFetcher<MatchingResultsNotification>(
            sbClient, sbSettings.RepeatSearchMatchingResultsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch repeat matching notification");
        _searchNotificationFetcher = new NotificationFetcher<SearchResultsNotification>(
            sbClient, sbSettings.RepeatSearchResultsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch repeat search notification");
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
        var result = AssertNotNull(response, "Repeat search API should have responded");

        result.WasSuccess.Should().BeTrue(
            "Repeat search request should have been accepted but got validation failures: {0}",
            string.Join(", ", result.ValidationFailures.Select(f => f.ErrorMessage)));

        return result.Response!.RepeatSearchIdentifier;
    }

    public async Task RepeatMatchingShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId,
        string searchId,
        DonorChanges donorChanges)
    {
        var notification = await FetchMatchingResultsNotification(repeatSearchId, searchId);
        notification.MatchingShouldHaveBeenSuccessful();

        var matchingResultSet = await _pollyRetry.ExecuteWithRetry(
            async () => await _blobHelper.DownloadResultSet<RepeatMatchingAlgorithmResultSet, MatchingAlgorithmResult>(
                notification.BlobStorageContainerName, notification.ResultsFileName, notification.BatchFolderName),
            _retry.FetchResultSet, $"Fetch repeat matching result set for repeat search '{repeatSearchId}'");
        AssertNotNull(matchingResultSet,
            "Matching result set should have been fetched for repeat search");

        ExpectedDonorsShouldNoLongerMatch(matchingResultSet!.NoLongerMatchingDonors, donorChanges.NoLongerMatching);

        foreach (var newlyMatchedDonor in donorChanges.NewlyMatching)
        {
            var donorResult = matchingResultSet.GetDonorResult(newlyMatchedDonor);
            await DonorResultShouldBeAsExpected(donorResult, "MatchingResult");
        }
    }

    public async Task RepeatSearchShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId,
        string searchId,
        DonorChanges donorChanges)
    {
        var notification = await FetchSearchResultsNotification(repeatSearchId, searchId);
        notification.SearchShouldHaveBeenSuccessful();

        var searchResultSet = await _pollyRetry.ExecuteWithRetry(
            async () => await _blobHelper.DownloadResultSet<RepeatSearchResultSet, SearchResult>(
                notification.BlobStorageContainerName, notification.ResultsFileName, notification.BatchFolderName),
            _retry.FetchResultSet, $"Fetch repeat search result set for repeat search '{repeatSearchId}'");
        AssertNotNull(searchResultSet,
            "Search result set should have been fetched for repeat search");

        ExpectedDonorsShouldNoLongerMatch(searchResultSet!.NoLongerMatchingDonorCodes, donorChanges.NoLongerMatching);

        foreach (var newlyMatchedDonor in donorChanges.NewlyMatching)
        {
            var donorResult = searchResultSet.GetDonorResult(newlyMatchedDonor);
            await DonorResultShouldBeAsExpected(donorResult, "SearchResult");
        }
    }

    public async Task RepeatRequestMissingRequiredInfoShouldReturnValidationErrors()
    {
        var response = await _pollyRetry.ExecuteWithRetry(
            async () => await _publicApiClient.PostRepeatSearchRequest(new RepeatSearchRequest()),
            _retry.ApiCall, "Submit invalid repeat search request (missing required fields)");
        var result = AssertNotNull(response, "Repeat search API should have responded");

        result.WasSuccess.Should().BeFalse(
            "Repeat search request should have been rejected with validation failures but was accepted");

        var validationErrors = result.ValidationFailures.ToList();
        validationErrors.ShouldContain("'Original Search Id' must not be empty.");
        validationErrors.ShouldContain("'Search Cutoff Date' must not be empty.");
    }

    private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string repeatSearchId, string searchId)
    {
        var notification = await _matchingNotificationFetcher.FetchNotification(
            m => m.RepeatSearchRequestId == repeatSearchId && m.SearchRequestId == searchId,
            $"Repeat search '{repeatSearchId}', original search '{searchId}'");
        return AssertNotNull(notification,
            $"Matching notification should have been received for repeat search {repeatSearchId}");
    }

    private async Task<SearchResultsNotification> FetchSearchResultsNotification(string repeatSearchId, string searchId)
    {
        var notification = await _searchNotificationFetcher.FetchNotification(
            m => m.RepeatSearchRequestId == repeatSearchId && m.SearchRequestId == searchId,
            $"Repeat search '{repeatSearchId}', original search '{searchId}'");
        return AssertNotNull(notification,
            $"Search notification should have been received for repeat search {repeatSearchId}");
    }

    private static void ExpectedDonorsShouldNoLongerMatch(
        IEnumerable<string> noLongerMatchingDonors,
        IEnumerable<string> expectedDonorCodes)
    {
        noLongerMatchingDonors.Should().Contain(expectedDonorCodes,
            "Expected donors should be in the no-longer-matching list");
    }
}
