using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

/// <summary>
/// One match prediction run of a search. Stored when the MatchPredictionStarted event comes in.
/// </summary>
[Table("SearchRequestMatchPredictions", Schema = "SearchTracking")]
public class SearchTrackingMatchPrediction
{
    public int Id { get; set; }

    public int SearchRequestId { get; set; }

    public DateTime InitiationTimeUtc { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime? PrepareBatches_StartTimeUtc { get; set; }

    public DateTime? PrepareBatches_EndTimeUtc { get; set; }

    /// <summary>Parallel match prediction: written by every batch, so it holds the last batch's time.</summary>
    public DateTime? AlgorithmCore_RunningBatches_StartTimeUtc { get; set; }

    /// <summary>Parallel match prediction: written by every batch, so it holds the last batch's time.</summary>
    public DateTime? AlgorithmCore_RunningBatches_EndTimeUtc { get; set; }

    public DateTime? PersistingResults_StartTimeUtc { get; set; }

    public DateTime? PersistingResults_EndTimeUtc { get; set; }

    public DateTime? CompletionTimeUtc { get; set; }

    public string? FailureInfo_Type { get; set; }

    public string? FailureInfo_Message { get; set; }

    public string? FailureInfo_ExceptionStacktrace { get; set; }

    public bool? IsSuccessful { get; set; }

    public bool IsParallelMatchPrediction { get; set; }

    /// <summary>The method the search asked for; null on rows from before the column was added.</summary>
    public bool? IsPrecomputedMatchPrediction { get; set; }
}
