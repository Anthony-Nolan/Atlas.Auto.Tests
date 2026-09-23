using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

[Table("DataRefreshHistory", Schema = "MatchingAlgorithmPersistent")]
public class DataRefreshRecord
{
    public int Id { get; set; }
    public DateTime? RefreshEndUtc { get; set; }

    [Required]
    public string Database { get; set; } = null!;

    public bool? WasSuccessful { get; set; }
}
