using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

[Table("HaplotypeFrequencySets", Schema = "MatchPrediction")]
public class HaplotypeFrequencySet
{
    public int Id { get; set; }

    public string? RegistryCode { get; set; }

    public string? EthnicityCode { get; set; }

    public bool Active { get; set; }
}
