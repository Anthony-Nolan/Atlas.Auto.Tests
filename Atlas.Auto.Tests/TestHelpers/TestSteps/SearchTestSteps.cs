using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class SearchTestSteps : SearchTestStepsBase
{
    private readonly NotificationFetcher<MatchingResultsNotification> _matchingNotificationFetcher;
    private readonly NotificationFetcher<SearchResultsNotification> _searchNotificationFetcher;

    private readonly ServiceBusClient _sbClient;
    private readonly ServiceBusSettings _sbSettings;

    /// <summary>Searches this test sent, by search ID.</summary>
    private readonly Dictionary<string, SubmittedSearch> _submittedSearches = new();

    public SearchTestSteps(
        IServiceProvider provider,
        DonorImportStepsForSearchTests donorImportSteps,
        ILogger logger,
        string testName)
        : base(provider, donorImportSteps, logger, testName)
    {
        var sbClient = provider.GetRequiredService<ServiceBusClient>();
        var sbSettings = provider.GetRequiredService<ServiceBusSettings>();
        _sbClient = sbClient;
        _sbSettings = sbSettings;
        _matchingNotificationFetcher = new NotificationFetcher<MatchingResultsNotification>(
            sbClient, sbSettings.MatchingResultsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch matching notification");
        _searchNotificationFetcher = new NotificationFetcher<SearchResultsNotification>(
            sbClient, sbSettings.SearchResultsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch search notification");
    }

    /// <param name="runMatchPrediction">Overrides RunMatchPrediction of the file when set.</param>
    public async Task<SearchInitiationResponse> SubmitSearchRequest(
        string searchRequestFileName,
        bool? parallelMatchPrediction = null,
        bool? usePrecomputedGenotypeSets = null,
        bool? runMatchPrediction = null)
    {
        var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);
        searchRequest.ParallelMatchPrediction = parallelMatchPrediction;
        searchRequest.UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets;
        searchRequest.RunMatchPrediction = runMatchPrediction ?? searchRequest.RunMatchPrediction;

        // Taken at every try, so it ends up as the time of the try that was accepted: search tracking stores that one
        var submittedAtUtc = DateTime.UtcNow;
        var response = await _pollyRetry.ExecuteWithRetry(
            async () =>
            {
                submittedAtUtc = DateTime.UtcNow;
                return await _publicApiClient.PostSearchRequest(searchRequest);
            },
            _retry.ApiCall, $"Submit search request '{searchRequestFileName}'");
        var result = AssertNotNull(response, "Search API should have responded");

        result.WasSuccess.Should().BeTrue(
            "Search request should have been accepted but got validation failures: {0}",
            string.Join(", ", result.ValidationFailures.Select(f => f.ErrorMessage)));

        _logger.LogInformation("Search request id: {SearchIdentifier}", result.Response!.SearchIdentifier);

        // Kept for the search tracking check: what was sent is what search tracking should have stored
        var searchIdentifier = result.Response.SearchIdentifier;
        _submittedSearches[searchIdentifier] = new SubmittedSearch(searchIdentifier, searchRequest, submittedAtUtc);
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

    public async Task MatchingShouldHaveFailedValidation(string searchRequestId, string expectedValidationError)
    {
        var notification = await FetchMatchingResultsNotification(searchRequestId);
        notification.MatchingShouldHaveFailedValidation(expectedValidationError);
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

    /// <summary>
    /// Matching succeeded, without checking what it found: for tests that check something else about the search.
    /// </summary>
    public async Task MatchingShouldHaveSucceeded(string searchRequestId)
    {
        var notification = await FetchMatchingResultsNotification(searchRequestId);
        notification.MatchingShouldHaveBeenSuccessful();
    }

    /// <summary>
    /// The search sent its final results, without checking what it found: for tests that check something else about
    /// the search.
    /// </summary>
    public async Task SearchShouldHaveSucceeded(string searchRequestId)
    {
        var notification = await FetchSearchResultsNotification(searchRequestId);
        notification.SearchShouldHaveBeenSuccessful();
    }

    /// <summary>
    /// The Search function has read the search's matching results from its subscription and processed them: it is
    /// then that it starts match prediction or, for a search without it, stops. A message that is no longer in the
    /// subscription and not in its dead-letter queue was processed.
    /// </summary>
    public async Task MatchPredictionShouldHaveReadMatchingResults(string searchRequestId)
    {
        var search = GetSubmittedSearch(searchRequestId);
        var subscription = $"{_sbSettings.MatchingResultsTopic}/{_sbSettings.MatchPredictionSubscription}";

        var read = await _pollyRetry.WaitUntil(
            async () => !await HasMatchingNotification(searchRequestId, deadLettered: false, search.SubmittedAtUtc),
            _retry.FetchMessages, $"Wait for the Search function to read the matching results of search '{searchRequestId}'");
        read.Should().BeTrue("the Search function should have read the matching results of search {0} from {1}",
            searchRequestId, subscription);

        var deadLettered = await HasMatchingNotification(searchRequestId, deadLettered: true, enqueuedSinceUtc: null);
        deadLettered.Should().BeFalse("the Search function should have processed the matching results of search {0}, " +
            "not dead-lettered them in {1}", searchRequestId, subscription);
    }

    /// <summary>
    /// For a search with match prediction: search tracking stored all of it (QA-1888).
    /// </summary>
    public async Task SearchShouldHaveBeenTracked(string searchRequestId)
    {
        var search = GetSubmittedSearch(searchRequestId);
        var matchingNotification = await FetchMatchingResultsNotification(searchRequestId);
        await SearchTracking.SearchShouldHaveBeenTracked(search, matchingNotification);
    }

    /// <summary>
    /// For a search that does not run match prediction: search tracking stored matching only.
    /// </summary>
    public async Task MatchingOnlySearchShouldHaveBeenTracked(string searchRequestId)
    {
        var search = GetSubmittedSearch(searchRequestId);
        var matchingNotification = await FetchMatchingResultsNotification(searchRequestId);
        await SearchTracking.MatchingOnlySearchShouldHaveBeenTracked(search, matchingNotification);
    }

    /// <param name="deadLettered">Read the dead-letter queue instead of the subscription.</param>
    /// <param name="enqueuedSinceUtc">Not for the dead-letter queue: see <see cref="ServiceBusPeeker.PeekMessages"/>.</param>
    private async Task<bool> HasMatchingNotification(string searchRequestId, bool deadLettered, DateTime? enqueuedSinceUtc)
    {
        await using var receiver = _sbClient.CreateReceiver(
            _sbSettings.MatchingResultsTopic,
            _sbSettings.MatchPredictionSubscription,
            new ServiceBusReceiverOptions { SubQueue = deadLettered ? SubQueue.DeadLetter : SubQueue.None });
        var enqueuedSince = enqueuedSinceUtc.HasValue ? new DateTimeOffset(enqueuedSinceUtc.Value, TimeSpan.Zero) : (DateTimeOffset?)null;
        var messages = await ServiceBusPeeker.PeekMessages(receiver, enqueuedSince);

        // A partly read queue may hide the notification, and "not there" would pass the check by mistake
        var source = $"{_sbSettings.MatchingResultsTopic}/{_sbSettings.MatchPredictionSubscription}" +
                     (deadLettered ? " dead-letter queue" : "");
        ServiceBusPeeker.ShouldHaveReadAll(messages, source, $"the matching notification of search {searchRequestId}");

        return messages
            .Select(m => JsonConvert.DeserializeObject<MatchingResultsNotification>(m.Body.ToString()))
            .Any(n => n?.SearchRequestId == searchRequestId);
    }

    private SubmittedSearch GetSubmittedSearch(string searchRequestId)
    {
        return _submittedSearches.TryGetValue(searchRequestId, out var search)
            ? search
            : throw new InvalidOperationException(
                $"Search {searchRequestId} was not sent by {nameof(SubmitSearchRequest)} of this test, " +
                "so the test does not know what search tracking should have stored.");
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
