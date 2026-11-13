using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

[Table("SearchRequestMatchingAlgorithmAttempts", Schema = "SearchTracking")]
public class SearchTrackingMatchingAlgorithmAttempt
{
    public int Id { get; set; }

    public int SearchRequestId { get; set; }

    public byte AttemptNumber { get; set; }

    public DateTime InitiationTimeUtc { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime? AlgorithmCore_Matching_StartTimeUtc { get; set; }

    public DateTime? AlgorithmCore_Matching_EndTimeUtc { get; set; }

    public DateTime? AlgorithmCore_Scoring_StartTimeUtc { get; set; }

    public DateTime? AlgorithmCore_Scoring_EndTimeUtc { get; set; }

    public DateTime? PersistingResults_StartTimeUtc { get; set; }

    public DateTime? PersistingResults_EndTimeUtc { get; set; }

    public DateTime? CompletionTimeUtc { get; set; }

    public bool? IsSuccessful { get; set; }

    public string? FailureInfo_Type { get; set; }

    public string? FailureInfo_Message { get; set; }

    public string? FailureInfo_ExceptionStacktrace { get; set; }
}
