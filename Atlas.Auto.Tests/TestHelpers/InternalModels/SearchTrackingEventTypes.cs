namespace Atlas.Auto.Tests.TestHelpers.InternalModels;

/// <summary>
/// EventType values of search-tracking-events messages (Atlas SearchTrackingEventType), as far as the tests need them.
/// </summary>
internal static class SearchTrackingEventTypes
{
    public const string SearchRequested = "SearchRequested";
    public const string MatchingAlgorithmAttemptStarted = "MatchingAlgorithmAttemptStarted";
    public const string MatchingAlgorithmCompleted = "MatchingAlgorithmCompleted";

    /// <summary>The only event that makes search tracking store a match prediction.</summary>
    public const string MatchPredictionStarted = "MatchPredictionStarted";

    /// <summary>Sent with the final results notification, the failure notification included.</summary>
    public const string MatchPredictionResultsSent = "MatchPredictionResultsSent";

    /// <summary>Events about one matching algorithm attempt: Atlas adds the AttemptNumber property to them.</summary>
    public static readonly IReadOnlySet<string> AttemptEvents = new HashSet<string>
    {
        MatchingAlgorithmAttemptStarted,
        MatchingAlgorithmCompleted
    };
}
