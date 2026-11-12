using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

/// <summary>
/// One search or repeat search as the search tracking function records it.
/// Only the columns the tests check; GUIDs are stored as lower-case strings, failure types as their enum names.
/// </summary>
[Table("SearchRequests", Schema = "SearchTracking")]
public class SearchTrackingRequest
{
    public int Id { get; set; }

    public string SearchIdentifier { get; set; } = null!;

    public bool IsRepeatSearch { get; set; }

    public string? OriginalSearchIdentifier { get; set; }

    public DateTime RequestTimeUtc { get; set; }

    public bool? MatchingAlgorithm_IsSuccessful { get; set; }

    public string? MatchingAlgorithm_FailureInfo_Type { get; set; }

    public string? MatchingAlgorithm_FailureInfo_Message { get; set; }

    public string? MatchingAlgorithm_FailureInfo_ExceptionStacktrace { get; set; }

    public byte? MatchingAlgorithm_TotalAttemptsNumber { get; set; }

    public int? MatchingAlgorithm_NumberOfResults { get; set; }

    public string? MatchingAlgorithm_HlaNomenclatureVersion { get; set; }

    public bool? MatchingAlgorithm_ResultsSent { get; set; }

    public DateTime? MatchingAlgorithm_ResultsSentTimeUtc { get; set; }

    public bool? MatchPrediction_IsSuccessful { get; set; }

    public string? MatchPrediction_FailureInfo_Type { get; set; }

    public string? MatchPrediction_FailureInfo_Message { get; set; }

    public string? MatchPrediction_FailureInfo_ExceptionStacktrace { get; set; }

    public int? MatchPrediction_DonorsPerBatch { get; set; }

    public int? MatchPrediction_TotalNumberOfBatches { get; set; }

    /// <summary>
    /// Set when the final search results notification is sent: the last event of a search.
    /// </summary>
    public bool? ResultsSent { get; set; }

    public DateTime? ResultsSentTimeUtc { get; set; }
}
