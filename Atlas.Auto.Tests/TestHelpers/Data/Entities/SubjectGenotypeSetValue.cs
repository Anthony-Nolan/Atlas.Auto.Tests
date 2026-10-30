using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

/// <summary>Value row: one stored genotype set, shared by every donor with the same typing, frequency set and loci.</summary>
[Table("SubjectGenotypeSetValues")]
public class SubjectGenotypeSetValue
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string HlaTypingKey { get; set; } = null!;

    public AllowedLociKey AllowedLociKey { get; set; }

    public int HaplotypeFrequencySetId { get; set; }

    public bool IsUnrepresented { get; set; }

    public byte[]? SubjectGenotypeSetData { get; set; }
}
