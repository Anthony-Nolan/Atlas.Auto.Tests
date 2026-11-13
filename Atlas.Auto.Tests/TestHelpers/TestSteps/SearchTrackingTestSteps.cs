using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
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
    /// Waits until search tracking has finished the search (results sent, every attempt completed),
    /// then checks that it stored one failed attempt and the failure.
    /// </summary>
    /// <param name="searchId">Search or repeat search ID.</param>
    /// <param name="originalSearchId">Null for a new search; the original search ID for a repeat search.</param>
    public async Task<SearchTrackingRequest> MatchingFailureShouldHaveBeenTracked(
        string searchId, ExpectedMatchingFailure expected, string? originalSearchId)
    {
        // Search request and attempts are stored by different events, so read both in one wait
        var tracking = await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var stored = await ReadTracking(searchId);
            return stored.IsFinished ? stored : null;
        }, _retry.CheckSearchTracking, $"Wait for search tracking to finish search '{searchId}'");

        if (tracking == null)
            await SearchTrackingShouldHaveFinished(searchId);

        _logger.LogInformation("Search tracking request id: {SearchTrackingRequestId}", tracking!.Request!.Id);
        tracking.Attempts.Should().ContainSingle("matching should have run once for search {0}", searchId);
        tracking.Request.MatchingShouldHaveFailed(expected, originalSearchId);
        tracking.Attempts.Single().ShouldHaveFailedBeforeMatching(expected);
        return tracking.Request;
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
    /// Every event of the search, in order, went through search-tracking-events with the custom properties
    /// Atlas sets: EventType, SearchIdentifier, OriginalSearchIdentifier, and AttemptNumber on attempt events.
    /// </summary>
    /// <param name="originalSearchId">Null for a new search; the original search ID for a repeat search.</param>
    /// <returns>The events, for checks that rely on what was not sent.</returns>
    public async Task<IReadOnlyCollection<SearchTrackingEventMessage>> EventsShouldHaveBeenSent(
        SearchTrackingRequest request, string? originalSearchId, IReadOnlyList<string> expectedEventTypes)
    {
        var events = await _eventReader.GetAuditedEvents(
            request.SearchIdentifier, new DateTimeOffset(request.RequestTimeUtc, TimeSpan.Zero));

        events.Select(e => e.EventType).Should().Equal(expectedEventTypes,
            "search {0} should have sent exactly these search tracking events", request.SearchIdentifier);

        using (new AssertionScope())
        {
            foreach (var trackingEvent in events)
                EventShouldHaveProperties(trackingEvent, originalSearchId);
        }

        return events;
    }

    /// <summary>
    /// Call after <see cref="MatchingFailureShouldHaveBeenTracked"/>: until then an event may still be redelivered.
    /// </summary>
    public async Task EventsShouldNotHaveBeenDeadLettered(SearchTrackingRequest request)
    {
        var deadLettered = await _eventReader.GetDeadLetteredEvents(request.SearchIdentifier);
        deadLettered.Select(e => e.EventType).Should().BeEmpty(
            "search tracking should have processed every event of search {0}", request.SearchIdentifier);
    }

    private async Task<StoredTracking> ReadTracking(string searchId)
    {
        var request = await _sqlHelper.GetSearchRequest(searchId);
        var attempts = request == null ? [] : await _sqlHelper.GetMatchingAlgorithmAttempts(request.Id);
        return new StoredTracking(request, attempts);
    }

    /// <summary>
    /// Fails with what search tracking stored and which events of the search it dead-lettered:
    /// a dead-lettered event is the usual reason a search never finishes.
    /// </summary>
    private async Task SearchTrackingShouldHaveFinished(string searchId)
    {
        var stored = await ReadTracking(searchId);
        var deadLettered = await _eventReader.GetDeadLetteredEvents(searchId);

        stored.IsFinished.Should().BeTrue(
            "search tracking should have finished search {0}; stored: request {1}, results sent {2}, " +
            "attempts {3} ({4} completed); dead-lettered events: [{5}]",
            searchId,
            stored.Request?.Id.ToString() ?? "none",
            stored.Request?.ResultsSent,
            stored.Attempts.Count,
            stored.Attempts.Count(a => a.CompletionTimeUtc != null),
            string.Join(", ", deadLettered.Select(e => e.EventType)));
    }

    /// <summary>What search tracking stored about a search: its request row and its matching algorithm attempts.</summary>
    private record StoredTracking(
        SearchTrackingRequest? Request, IReadOnlyCollection<SearchTrackingMatchingAlgorithmAttempt> Attempts)
    {
        /// <summary>The last event (results sent) is stored and every attempt has completed.</summary>
        public bool IsFinished =>
            Request?.ResultsSent == true && Attempts.Count > 0 && Attempts.All(a => a.CompletionTimeUtc != null);
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
