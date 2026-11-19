using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Client.Models.Search.Results.Matching;
using Azure.Messaging.ServiceBus;
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
    /// the search sent (see <see cref="SearchEventsShouldHaveBeenSent"/>) and none dead-lettered.
    /// </summary>
    /// <param name="expectedSearchCriteria">As the request file asks for, e.g. "10/10".</param>
    /// <param name="matchingNotification">Matching results notification of the search: the stored counts must match it.</param>
    public async Task SearchShouldHaveBeenTracked(
        SubmittedSearch search, string expectedSearchCriteria, MatchingResultsNotification matchingNotification)
    {
        var tracking = await WaitForTracking(search.SearchIdentifier, t => t.IsMatchPredictionFinished,
            "results sent, matching attempt and match prediction completed");
        var request = tracking.Request!;

        // Request row: the search as sent
        var searchedRequest = request.ShouldHoldSubmittedSearch(search, expectedSearchCriteria);

        // Matching: one successful attempt
        tracking.Attempts.Should().ContainSingle("matching should have run once for search {0}", search.SearchIdentifier);
        tracking.Attempts.Single().ShouldHaveSucceeded();
        request.MatchingShouldHaveSucceeded(matchingNotification);

        // Match prediction: one successful run, then the final results were sent
        tracking.MatchPredictions.Should().ContainSingle("match prediction should have run once for search {0}", search.SearchIdentifier);
        var matchPrediction = tracking.MatchPredictions.Single();
        matchPrediction.ShouldHaveSucceeded(searchedRequest);
        request.MatchPredictionShouldHaveSucceeded(matchPrediction);

        // Events and dead-letter queue
        await SearchEventsShouldHaveBeenSent(request, matchPrediction.IsParallelMatchPrediction);
        await EventsShouldNotHaveBeenDeadLettered(request);
    }

    /// <summary>
    /// QA-1888, search with RunMatchPrediction false: waits until search tracking has finished matching, then checks
    /// that only matching was stored and sent events. Call once the Search function has read the matching results:
    /// until then match prediction could still start.
    /// </summary>
    /// <param name="expectedSearchCriteria">As the request file asks for, e.g. "10/10".</param>
    /// <param name="matchingNotification">Matching results notification of the search: the stored counts must match it.</param>
    public async Task MatchingOnlySearchShouldHaveBeenTracked(
        SubmittedSearch search, string expectedSearchCriteria, MatchingResultsNotification matchingNotification)
    {
        var tracking = await WaitForTracking(search.SearchIdentifier, t => t.IsMatchingFinished,
            "matching results sent and matching attempt completed");
        var request = tracking.Request!;

        // Request row: the search as sent
        request.ShouldHoldSubmittedSearch(search, expectedSearchCriteria);

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

    /// <summary>
    /// Every event of a search with match prediction, in any order. Stages that Atlas retries from the start send
    /// their Started/Ended pair again at every try, so those pairs are counted on their own; see
    /// <see cref="RetriedStagePairs"/>. Every other event, results sent included, must come exactly as listed.
    /// </summary>
    private async Task SearchEventsShouldHaveBeenSent(SearchTrackingRequest request, bool isParallel)
    {
        var batches = isParallel ? request.MatchPrediction_TotalNumberOfBatches!.Value : 1;
        List<string> expected =
        [
            ..SearchTrackingEventTypes.SuccessfulMatching,
            ..SearchTrackingEventTypes.SuccessfulMatchPrediction(batches)
        ];
        var sent = (await ReadEventsWithProperties(request, originalSearchId: null)).Select(e => e.EventType).ToList();

        // Every event outside the retried pairs: exactly as listed
        var retriedPairs = RetriedStagePairs(isParallel, batches);
        var retriedEvents = retriedPairs.SelectMany(p => new[] { p.Started, p.Ended }).ToHashSet();
        sent.Where(e => !retriedEvents.Contains(e!)).Should().BeEquivalentTo(expected.Where(e => !retriedEvents.Contains(e)),
            "search {0} should have sent exactly these search tracking events, each as many times as listed",
            request.SearchIdentifier);

        // Retried pairs: every try sends Started, a try that gets past its work sends Ended
        foreach (var (started, ended, atLeast) in retriedPairs)
        {
            var startedCount = sent.Count(e => e == started);
            var endedCount = sent.Count(e => e == ended);

            startedCount.Should().BeGreaterThanOrEqualTo(atLeast, "search {0} should have sent {1} at least {2} times",
                request.SearchIdentifier, started, atLeast);
            endedCount.Should().BeGreaterThanOrEqualTo(atLeast, "search {0} should have sent {1} at least {2} times",
                request.SearchIdentifier, ended, atLeast);
            endedCount.Should().BeLessThanOrEqualTo(startedCount, "a try of search {0} sends {1} only after its {2}",
                request.SearchIdentifier, ended, started);

            // Not a failure, but worth seeing in pipeline logs when a test turns flaky
            if (startedCount > atLeast)
                _logger.LogInformation(
                    "Search {SearchId}: {Started} sent {StartedCount} times, {Ended} {EndedCount}, expected {AtLeast}: the stage was retried",
                    request.SearchIdentifier, started, startedCount, ended, endedCount, atLeast);
        }
    }

    /// <summary>
    /// Started/Ended pairs that a successful search can send more than once:
    /// <list type="bullet">
    /// <item>batch preparation and persisting results: durable activities with retry (up to 5 tries), each sends
    /// Started first and Ended after its work, so a failure after an event repeats the activity from the start;</item>
    /// <item>running batches, parallel only: the worker sends both at every delivery of a batch (Ended goes before the
    /// batch result is published, so a failed publish brings both again). Sequential sends them once, from activities
    /// that only send the event, so that pair stays in the exact list.</item>
    /// </list>
    /// </summary>
    /// <returns>Each pair with how many times it must come at least: once per stage, once per batch.</returns>
    private static List<(string Started, string Ended, int AtLeast)> RetriedStagePairs(bool isParallel, int batches)
    {
        List<(string Started, string Ended, int AtLeast)> pairs =
        [
            (SearchTrackingEventTypes.MatchPredictionBatchPreparationStarted, SearchTrackingEventTypes.MatchPredictionBatchPreparationEnded, 1),
            (SearchTrackingEventTypes.MatchPredictionPersistingResultsStarted, SearchTrackingEventTypes.MatchPredictionPersistingResultsEnded, 1)
        ];
        if (isParallel)
            pairs.Add((SearchTrackingEventTypes.MatchPredictionRunningBatchesStarted, SearchTrackingEventTypes.MatchPredictionRunningBatchesEnded, batches));
        return pairs;
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
        catch (Exception e) when (e is InvalidOperationException or ServiceBusException)
        {
            // Too full to read in full, or Service Bus failed just now: the stored state still goes into the message
            return $"not read ({e.GetType().Name}: {e.Message})";
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
        /// Like <see cref="IsFinished"/>, and match prediction has completed too: parallel match prediction sends its
        /// completion after the results. Search tracking stores the completion in two commits, first the request row
        /// (MatchPrediction_IsSuccessful, batch counts), then the match prediction row (CompletionTimeUtc), and the
        /// tables are read one after another: the request row must hold the first commit in this same read, or a read
        /// between the two could pass with the request fields still empty.
        /// </summary>
        public bool IsMatchPredictionFinished =>
            IsFinished
            && Request!.MatchPrediction_IsSuccessful != null
            && MatchPredictions.Count > 0 && MatchPredictions.All(p => p.CompletionTimeUtc != null);

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
