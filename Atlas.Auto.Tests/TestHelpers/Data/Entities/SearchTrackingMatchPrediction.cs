using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

/// <summary>
/// Only the keys: the tests check whether match prediction ran for a search, not its timings.
/// </summary>
[Table("SearchRequestMatchPredictions", Schema = "SearchTracking")]
public class SearchTrackingMatchPrediction
{
    public int Id { get; set; }

    public int SearchRequestId { get; set; }
}
