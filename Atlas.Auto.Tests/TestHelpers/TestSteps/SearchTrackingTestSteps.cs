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
    /// Waits until search tracking has stored the search's last event (results sent), so later checks see every event.
    /// </summary>
    /// <param name="searchId">Search or repeat search ID.</param>
    public async Task<SearchTrackingRequest> SearchShouldHaveFinished(string searchId)
    {
        var request = await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var found = await _sqlHelper.GetSearchRequest(searchId);
            return found?.ResultsSent == true ? found : null;
        }, _retry.CheckSearchTracking, $"Wait for search tracking to store results sent for search '{searchId}'");

        request.Should().NotBeNull(
            "search tracking should have stored that the results of search {0} were sent", searchId);
        _logger.LogInformation("Search tracking request id: {SearchTrackingRequestId}", request!.Id);
        return request;
    }

    public async Task OneFailedAttemptShouldHaveBeenStored(SearchTrackingRequest request, ExpectedMatchingFailure expected)
    {
        var attempts = await _sqlHelper.GetMatchingAlgorithmAttempts(request.Id);
        attempts.Should().ContainSingle("matching should have run once for search {0}", request.SearchIdentifier);
        attempts.Single().ShouldHaveFailedBeforeMatching(expected);
    }

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
    /// Call after <see cref="SearchShouldHaveFinished"/>: until then an event may still be redelivered.
    /// </summary>
    public async Task EventsShouldNotHaveBeenDeadLettered(SearchTrackingRequest request)
    {
        var deadLettered = await _eventReader.GetDeadLetteredEvents(request.SearchIdentifier);
        deadLettered.Select(e => e.EventType).Should().BeEmpty(
            "search tracking should have processed every event of search {0}", request.SearchIdentifier);
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
