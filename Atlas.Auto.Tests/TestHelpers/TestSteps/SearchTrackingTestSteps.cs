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
    /// Waits until search tracking has stored both the search's last event (results sent) and its one failed attempt,
    /// then checks what it stored about the failure.
    /// </summary>
    /// <param name="searchId">Search or repeat search ID.</param>
    /// <param name="originalSearchId">Null for a new search; the original search ID for a repeat search.</param>
    public async Task<SearchTrackingRequest> MatchingFailureShouldHaveBeenTracked(
        string searchId, ExpectedMatchingFailure expected, string? originalSearchId)
    {
        // Search request and attempt are stored by different events, so wait for both in one loop
        var tracked = await _pollyRetry.ExecuteWithRetry(
            () => ReadTrackedFailure(searchId, onlyWhenStored: true),
            _retry.CheckSearchTracking, $"Wait for search tracking to store the failure of search '{searchId}'");

        // Timed out: read what is there, so the failure shows it
        tracked ??= await ReadTrackedFailure(searchId, onlyWhenStored: false);

        tracked!.Request.Should().NotBeNull("search tracking should have stored search {0}", searchId);
        _logger.LogInformation("Search tracking request id: {SearchTrackingRequestId}", tracked.Request!.Id);
        tracked.Attempts.Should().ContainSingle("matching should have run once for search {0}", searchId);

        tracked.Request.MatchingShouldHaveFailed(expected, originalSearchId);
        tracked.Attempts.Single().ShouldHaveFailedBeforeMatching(expected);
        return tracked.Request;
    }

    /// <summary>
    /// Call after <see cref="EventsShouldHaveBeenSent"/> has shown that no MatchPredictionStarted event was sent:
    /// only that event makes search tracking store a match prediction, so after it the table cannot change.
    /// </summary>
    public async Task MatchPredictionShouldNotHaveBeenStored(SearchTrackingRequest request)
    {
        var matchPredictions = await _sqlHelper.GetMatchPredictions(request.Id);
        matchPredictions.Should().BeEmpty(
            "match prediction should not run when matching failed for search {0}", request.SearchIdentifier);
    }

    /// <summary>
    /// Every event of the search, in order, went through search-tracking-events with the custom properties
    /// Atlas sets: EventType, SearchIdentifier, OriginalSearchIdentifier, and AttemptNumber on attempt events.
    /// </summary>
    /// <param name="originalSearchId">Null for a new search; the original search ID for a repeat search.</param>
    public async Task EventsShouldHaveBeenSent(
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

    /// <param name="onlyWhenStored">
    /// True: null until results are sent and exactly one failed attempt is stored. False: whatever is stored now.
    /// </param>
    private async Task<TrackedFailure?> ReadTrackedFailure(string searchId, bool onlyWhenStored)
    {
        var request = await _sqlHelper.GetSearchRequest(searchId);
        var attempts = request == null ? [] : await _sqlHelper.GetMatchingAlgorithmAttempts(request.Id);

        var stored = request?.ResultsSent == true && attempts.Count == 1 && attempts.Single().IsSuccessful == false;
        return stored || !onlyWhenStored ? new TrackedFailure(request, attempts) : null;
    }

    private record TrackedFailure(
        SearchTrackingRequest? Request, IReadOnlyCollection<SearchTrackingMatchingAlgorithmAttempt> Attempts);

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
