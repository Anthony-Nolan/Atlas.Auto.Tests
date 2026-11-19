namespace Atlas.Auto.Tests.TestHelpers.InternalModels;

/// <summary>
/// EventType values of search-tracking-events messages (Atlas SearchTrackingEventType).
/// </summary>
internal static class SearchTrackingEventTypes
{
    public const string SearchRequested = "SearchRequested";

    public const string MatchingAlgorithmAttemptStarted = "MatchingAlgorithmAttemptStarted";
    public const string MatchingAlgorithmCoreMatchingStarted = "MatchingAlgorithmCoreMatchingStarted";
    public const string MatchingAlgorithmCoreMatchingEnded = "MatchingAlgorithmCoreMatchingEnded";
    public const string MatchingAlgorithmCoreScoringStarted = "MatchingAlgorithmCoreScoringStarted";
    public const string MatchingAlgorithmCoreScoringEnded = "MatchingAlgorithmCoreScoringEnded";
    public const string MatchingAlgorithmPersistingResultsStarted = "MatchingAlgorithmPersistingResultsStarted";
    public const string MatchingAlgorithmPersistingResultsEnded = "MatchingAlgorithmPersistingResultsEnded";
    public const string MatchingAlgorithmCompleted = "MatchingAlgorithmCompleted";

    /// <summary>The only event that makes search tracking store a match prediction.</summary>
    public const string MatchPredictionStarted = "MatchPredictionStarted";
    public const string MatchPredictionBatchPreparationStarted = "MatchPredictionBatchPreparationStarted";
    public const string MatchPredictionBatchPreparationEnded = "MatchPredictionBatchPreparationEnded";

    /// <summary>
    /// Sequential match prediction sends it once, parallel match prediction at every try of a batch: once per batch,
    /// more when a batch is redelivered.
    /// </summary>
    public const string MatchPredictionRunningBatchesStarted = "MatchPredictionRunningBatchesStarted";

    /// <summary>Sequential match prediction sends it once, parallel match prediction once per successful batch.</summary>
    public const string MatchPredictionRunningBatchesEnded = "MatchPredictionRunningBatchesEnded";

    public const string MatchPredictionPersistingResultsStarted = "MatchPredictionPersistingResultsStarted";
    public const string MatchPredictionPersistingResultsEnded = "MatchPredictionPersistingResultsEnded";
    public const string MatchPredictionCompleted = "MatchPredictionCompleted";

    /// <summary>Sent with the final results notification, the failure notification included.</summary>
    public const string MatchPredictionResultsSent = "MatchPredictionResultsSent";

    /// <summary>
    /// Events about one matching algorithm attempt: Atlas adds the AttemptNumber property to them
    /// (ISearchTrackingMatchingAttemptEvent).
    /// </summary>
    public static readonly IReadOnlySet<string> AttemptEvents = new HashSet<string>
    {
        MatchingAlgorithmAttemptStarted,
        MatchingAlgorithmCoreMatchingStarted,
        MatchingAlgorithmCoreMatchingEnded,
        MatchingAlgorithmCoreScoringStarted,
        MatchingAlgorithmCoreScoringEnded,
        MatchingAlgorithmPersistingResultsStarted,
        MatchingAlgorithmPersistingResultsEnded,
        MatchingAlgorithmCompleted
    };

    /// <summary>
    /// How many times core matching runs in one attempt that found results. Atlas bug ATL-573: scoring counts the lazy
    /// matching results (CountAsync in MatchScoringService.StreamScoringUnranked), so matching runs a second time. Set it
    /// back to 1 once ATL-573 is fixed.
    /// </summary>
    private const int CoreMatchingRuns = 2;

    /// <summary>Every event of a search whose matching found results on its first attempt.</summary>
    public static readonly IReadOnlyList<string> SuccessfulMatching =
    [
        SearchRequested,
        MatchingAlgorithmAttemptStarted,
        ..Enumerable.Repeat(MatchingAlgorithmCoreMatchingStarted, CoreMatchingRuns),
        ..Enumerable.Repeat(MatchingAlgorithmCoreMatchingEnded, CoreMatchingRuns),
        MatchingAlgorithmCoreScoringStarted,
        MatchingAlgorithmCoreScoringEnded,
        MatchingAlgorithmPersistingResultsStarted,
        MatchingAlgorithmPersistingResultsEnded,
        MatchingAlgorithmCompleted
    ];

    /// <summary>
    /// Every event of a successful match prediction run, each batch tried once (a redelivered parallel batch sends
    /// RunningBatchesStarted once more).
    /// </summary>
    /// <param name="batchRuns">How many times the batches were run: 1 for sequential, the batch count for parallel.</param>
    public static IReadOnlyList<string> SuccessfulMatchPrediction(int batchRuns) =>
    [
        MatchPredictionStarted,
        MatchPredictionBatchPreparationStarted,
        MatchPredictionBatchPreparationEnded,
        ..Enumerable.Repeat(MatchPredictionRunningBatchesStarted, batchRuns),
        ..Enumerable.Repeat(MatchPredictionRunningBatchesEnded, batchRuns),
        MatchPredictionPersistingResultsStarted,
        MatchPredictionPersistingResultsEnded,
        MatchPredictionResultsSent,
        MatchPredictionCompleted
    ];
}
