using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Client.Models.Search.Results.Matching;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

/// <summary>
/// Checks what the search tracking function recorded about a search: SearchTracking tables and search-tracking-events messages.
/// </summary>
internal class SearchTrackingTestSteps
{
    private readonly ILogger _logger;
    private readonly SearchTrackingSqlHelper _sqlHelper;
    private readonly SearchTrackingEventReader _eventReader;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;

    public SearchTrackingTestSteps(IServiceProvider provider, ILogger logger)
    {
        _logger = logger;
        _sqlHelper = provider.GetRequiredService<SearchTrackingSqlHelper>();
        _eventReader = provider.GetRequiredService<SearchTrackingEventReader>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
    }

    /// <summary>
    /// QA-1888: waits until search tracking has finished a search with match prediction, then checks everything it
    /// stored: the request as sent, one successful matching attempt, one successful match prediction, every event of
    /// the search sent once (batch events once per batch run) and none dead-lettered.
    /// </summary>
    /// <param name="matchingNotification">Matching results notification of the search: the stored counts must match it.</param>
    public async Task SearchShouldHaveBeenTracked(SubmittedSearch search, MatchingResultsNotification matchingNotification)
    {
        var tracking = await WaitForTracking(search.SearchIdentifier, t => t.IsMatchPredictionFinished,
            "results sent, matching attempt and match prediction completed");
        var request = tracking.Request!;

        // Request row: the search as sent
        var searchedRequest = request.ShouldHoldSubmittedSearch(search);

        // Matching: one successful attempt
        tracking.Attempts.Should().ContainSingle("matching should have run once for search {0}", search.SearchIdentifier);
        tracking.Attempts.Single().ShouldHaveSucceeded();
        request.MatchingShouldHaveSucceeded(matchingNotification);

        // Match prediction: one successful run, then the final results were sent
        tracking.MatchPredictions.Should().ContainSingle("match prediction should have run once for search {0}", search.SearchIdentifier);
        var matchPrediction = tracking.MatchPredictions.Single();
        matchPrediction.ShouldHaveSucceeded(searchedRequest);
        request.MatchPredictionShouldHaveSucceeded(matchPrediction);

        // Events: parallel match prediction runs every batch on its own, sequential runs them all in one go
        var batchRuns = matchPrediction.IsParallelMatchPrediction ? request.MatchPrediction_TotalNumberOfBatches!.Value : 1;
        List<string> expected =
        [
            ..SearchTrackingEventTypes.SuccessfulMatching,
            ..SearchTrackingEventTypes.SuccessfulMatchPrediction(batchRuns)
        ];
        var sent = (await ReadEventsWithProperties(request, originalSearchId: null)).Select(e => e.EventType!).ToList();

        // Every event except RunningBatchesStarted: exactly as listed, in any order
        const string batchStarted = SearchTrackingEventTypes.MatchPredictionRunningBatchesStarted;
        sent.Where(e => e != batchStarted).Should().BeEquivalentTo(expected.Where(e => e != batchStarted),
            "search {0} should have sent exactly these search tracking events, each as many times as listed",
            request.SearchIdentifier);

        // RunningBatchesStarted: a parallel batch sends it at every try, so a redelivered batch that then succeeds
        // sends it once more (RunningBatchesEnded only on success, checked above)
        var batchStartedCount = sent.Count(e => e == batchStarted);
        if (matchPrediction.IsParallelMatchPrediction)
            batchStartedCount.Should().BeGreaterThanOrEqualTo(batchRuns,
                "every batch of search {0} should have sent {1} at least once", request.SearchIdentifier, batchStarted);
        else
            batchStartedCount.Should().Be(batchRuns,
                "sequential match prediction of search {0} should have sent {1} once", request.SearchIdentifier, batchStarted);

        await EventsShouldNotHaveBeenDeadLettered(request);
    }

    /// <summary>
    /// QA-1888, search with RunMatchPrediction false: waits until search tracking has finished matching, then checks
    /// that only matching was stored and sent events. Call once the Search function has read the matching results:
    /// until then match prediction could still start.
    /// </summary>
    /// <param name="matchingNotification">Matching results notification of the search: the stored counts must match it.</param>
    public async Task MatchingOnlySearchShouldHaveBeenTracked(SubmittedSearch search, MatchingResultsNotification matchingNotification)
    {
        var tracking = await WaitForTracking(search.SearchIdentifier, t => t.IsMatchingFinished,
            "matching results sent and matching attempt completed");
        var request = tracking.Request!;

        // Request row: the search as sent
        request.ShouldHoldSubmittedSearch(search);

        // Matching: one successful attempt
        tracking.Attempts.Should().ContainSingle("matching should have run once for search {0}", search.SearchIdentifier);
        tracking.Attempts.Single().ShouldHaveSucceeded();
        request.MatchingShouldHaveSucceeded(matchingNotification);

        // Match prediction: nothing stored, no final results
        request.MatchPredictionShouldNotHaveRun();
        var matchPredictions = await _sqlHelper.GetMatchPredictions(request.Id);
        matchPredictions.Should().BeEmpty("match prediction should not run for search {0}", search.SearchIdentifier);

        // Events: matching only
        await EventsShouldHaveBeenSentInAnyOrder(request, SearchTrackingEventTypes.SuccessfulMatching);
        await EventsShouldNotHaveBeenDeadLettered(request);
    }

    /// <summary>
    /// Waits until search tracking has finished the search (results sent, every attempt completed),
    /// then checks that it stored one failed attempt and the failure.
    /// </summary>
    /// <param name="searchId">Search or repeat search ID.</param>
    /// <param name="originalSearchId">Null for a new search; the original search ID for a repeat search.</param>
    public async Task<SearchTrackingRequest> MatchingFailureShouldHaveBeenTracked(
        string searchId, ExpectedMatchingFailure expected, string? originalSearchId)
    {
        var tracking = await WaitForTracking(searchId, t => t.IsFinished, "results sent and every attempt completed");

        var request = tracking.Request!;

        tracking.Attempts.Should().ContainSingle("matching should have run once for search {0}", searchId);
        request.MatchingShouldHaveFailed(expected, originalSearchId);
        tracking.Attempts.Single().ShouldHaveFailedBeforeMatching(expected);
        return request;
    }

    /// <summary>
    /// The attempt starts when matching runs; its initiation time is the first request of the search.
    /// Call after <see cref="MatchingFailureShouldHaveBeenTracked"/>, which checks there is one attempt.
    /// </summary>
    public async Task AttemptShouldHaveStartedAfterWait(SearchTrackingRequest request, TimeSpan minWait, TimeSpan maxWait)
    {
        var attempt = (await _sqlHelper.GetMatchingAlgorithmAttempts(request.Id)).Single();
        var waited = attempt.StartTimeUtc - attempt.InitiationTimeUtc;
        _logger.LogInformation("Search {SearchId} waited {Waited} before matching", request.SearchIdentifier, waited);

        waited.Should().BeGreaterThanOrEqualTo(minWait,
            "matching should have waited for the original results before it ran for search {0}", request.SearchIdentifier);
        waited.Should().BeLessThanOrEqualTo(maxWait,
            "matching should have run once the maximum wait was over for search {0}", request.SearchIdentifier);
    }

    /// <summary>
    /// Only a MatchPredictionStarted event makes search tracking store a match prediction. The events the search sent
    /// have none, so the table is checked when it can no longer change.
    /// </summary>
    /// <param name="sentEvents">What <see cref="EventsShouldHaveBeenSent"/> returned.</param>
    public async Task MatchPredictionShouldNotHaveBeenStored(
        SearchTrackingRequest request, IReadOnlyCollection<SearchTrackingEventMessage> sentEvents)
    {
        sentEvents.Select(e => e.EventType).Should().NotContain(SearchTrackingEventTypes.MatchPredictionStarted,
            "match prediction should not start when matching failed for search {0}", request.SearchIdentifier);

        var matchPredictions = await _sqlHelper.GetMatchPredictions(request.Id);
        matchPredictions.Should().BeEmpty(
            "match prediction should not run when matching failed for search {0}", request.SearchIdentifier);
    }

    /// <summary>
    /// Every event of the search, in this order, went through search-tracking-events with the custom properties
    /// Atlas sets: EventType, SearchIdentifier, OriginalSearchIdentifier, and AttemptNumber on attempt events.
    /// </summary>
    /// <param name="originalSearchId">Null for a new search; the original search ID for a repeat search.</param>
    /// <returns>The events, for checks that rely on what was not sent.</returns>
    public async Task<IReadOnlyCollection<SearchTrackingEventMessage>> EventsShouldHaveBeenSent(
        SearchTrackingRequest request, string? originalSearchId, IReadOnlyList<string> expectedEventTypes)
    {
        var events = await ReadEventsWithProperties(request, originalSearchId);
        events.Select(e => e.EventType).Should().Equal(expectedEventTypes,
            "search {0} should have sent exactly these search tracking events", request.SearchIdentifier);
        return events;
    }

    /// <summary>
    /// Call after search tracking has finished the search: until then an event may still be redelivered.
    /// </summary>
    public async Task EventsShouldNotHaveBeenDeadLettered(SearchTrackingRequest request)
    {
        var deadLettered = await _eventReader.GetDeadLetteredEvents(request.SearchIdentifier);
        deadLettered.Select(e => e.EventType).Should().BeEmpty(
            "search tracking should have processed every event of search {0}", request.SearchIdentifier);
    }

    /// <summary>
    /// Like <see cref="EventsShouldHaveBeenSent"/>, but in any order: matching sends MatchingAlgorithmCompleted after
    /// its results notification, so match prediction may have started before it.
    /// </summary>
    private async Task EventsShouldHaveBeenSentInAnyOrder(SearchTrackingRequest request, IReadOnlyList<string> expectedEventTypes)
    {
        var events = await ReadEventsWithProperties(request, originalSearchId: null);
        events.Select(e => e.EventType).Should().BeEquivalentTo(expectedEventTypes,
            "search {0} should have sent exactly these search tracking events, each as many times as listed",
            request.SearchIdentifier);
    }

    private async Task<IReadOnlyCollection<SearchTrackingEventMessage>> ReadEventsWithProperties(
        SearchTrackingRequest request, string? originalSearchId)
    {
        var events = await _eventReader.GetAuditedEvents(
            request.SearchIdentifier, new DateTimeOffset(request.RequestTimeUtc, TimeSpan.Zero));

        using (new AssertionScope())
        {
            foreach (var trackingEvent in events)
                EventShouldHaveProperties(trackingEvent, originalSearchId);
        }

        return events;
    }

    /// <summary>
    /// Search request, attempts and match prediction are stored by different events, so all are read in one wait.
    /// </summary>
    /// <param name="waitingFor">What <paramref name="isDone"/> waits for, for the log and the failure message.</param>
    private async Task<StoredTracking> WaitForTracking(string searchId, Func<StoredTracking, bool> isDone, string waitingFor)
    {
        var tracking = await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var stored = await ReadTracking(searchId);
            return isDone(stored) ? stored : null;
        }, _retry.CheckSearchTracking, $"Wait for search tracking of search '{searchId}': {waitingFor}");

        // One more read after the wait: tracking may have finished just after the last try
        tracking ??= await ReadTracking(searchId);
        if (!isDone(tracking))
            await SearchTrackingShouldHaveFinished(searchId, tracking, waitingFor);

        _logger.LogInformation("Search tracking request id: {SearchTrackingRequestId}", tracking.Request!.Id);
        return tracking;
    }

    private async Task<StoredTracking> ReadTracking(string searchId)
    {
        var request = await _sqlHelper.GetSearchRequest(searchId);
        if (request == null)
            return new StoredTracking(null, [], []);

        var attempts = await _sqlHelper.GetMatchingAlgorithmAttempts(request.Id);
        var matchPredictions = await _sqlHelper.GetMatchPredictions(request.Id);
        return new StoredTracking(request, attempts, matchPredictions);
    }

    /// <summary>
    /// Fails with what search tracking stored and which events of the search it dead-lettered:
    /// a dead-lettered event is the usual reason a search never finishes.
    /// </summary>
    /// <param name="stored">The last read, which is not finished.</param>
    private async Task SearchTrackingShouldHaveFinished(string searchId, StoredTracking stored, string waitingFor)
    {
        var deadLettered = await DescribeDeadLetteredEvents(searchId);

        Assert.Fail(
            $"Search tracking should have finished search {searchId} ({waitingFor}); stored: " +
            $"request {stored.Request?.Id.ToString() ?? "none"}, " +
            $"matching results sent {stored.Request?.MatchingAlgorithm_ResultsSent}, " +
            $"results sent {stored.Request?.ResultsSent}, " +
            $"attempts {stored.Attempts.Count} ({stored.Attempts.Count(a => a.CompletionTimeUtc != null)} completed), " +
            $"match predictions {stored.MatchPredictions.Count} " +
            $"({stored.MatchPredictions.Count(p => p.CompletionTimeUtc != null)} completed); " +
            $"dead-lettered events: {deadLettered}");
    }

    /// <summary>
    /// For the failure message only: when the dead-letter queue cannot be read in full, the message still tells what
    /// was stored and why the dead-lettered events are missing.
    /// </summary>
    private async Task<string> DescribeDeadLetteredEvents(string searchId)
    {
        try
        {
            var deadLettered = await _eventReader.GetDeadLetteredEvents(searchId);
            return $"[{string.Join(", ", deadLettered.Select(e => e.EventType))}]";
        }
        catch (InvalidOperationException e)
        {
            return $"not read ({e.Message})";
        }
    }

    /// <summary>What search tracking stored about a search: its request row, matching attempts and match predictions.</summary>
    private record StoredTracking(
        SearchTrackingRequest? Request,
        IReadOnlyCollection<SearchTrackingMatchingAlgorithmAttempt> Attempts,
        IReadOnlyCollection<SearchTrackingMatchPrediction> MatchPredictions)
    {
        /// <summary>The last event (results sent) is stored and every attempt has completed.</summary>
        public bool IsFinished => Request?.ResultsSent == true && AttemptsCompleted;

        /// <summary>Matching sent its results and every attempt has completed.</summary>
        public bool IsMatchingFinished => Request?.MatchingAlgorithm_ResultsSent == true && AttemptsCompleted;

        /// <summary>
        /// Like <see cref="IsFinished"/>, and match prediction has completed too:
        /// parallel match prediction sends its completion after the results.
        /// </summary>
        public bool IsMatchPredictionFinished =>
            IsFinished && MatchPredictions.Count > 0 && MatchPredictions.All(p => p.CompletionTimeUtc != null);

        private bool AttemptsCompleted => Attempts.Count > 0 && Attempts.All(a => a.CompletionTimeUtc != null);
    }

    private static void EventShouldHaveProperties(SearchTrackingEventMessage trackingEvent, string? originalSearchId)
    {
        var expectedAttemptNumber = SearchTrackingEventTypes.AttemptEvents.Contains(trackingEvent.EventType!) ? "1" : null;

        trackingEvent.HasOriginalSearchIdentifier.Should().BeTrue(
            "{0} should have the {1} property", trackingEvent.EventType, SearchTrackingEventMessage.OriginalSearchIdentifierProperty);
        trackingEvent.OriginalSearchIdentifier.Should().Be(originalSearchId?.ToLowerInvariant(),
            "{0} should have the original search ID of a repeat search only", trackingEvent.EventType);
        trackingEvent.AttemptNumber.Should().Be(expectedAttemptNumber,
            "only attempt events should have an attempt number; {0}", trackingEvent.EventType);
    }
}
