using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

/// <summary>The 4 allowed-loci combinations a donor has a precomputed genotype set for. Stored as the member name.</summary>
public enum AllowedLociKey { ABCDrb1Dqb1 = 1, ABCDrb1 = 2, ABDrb1Dqb1 = 3, ABDrb1 = 4 }

/// <summary>Link row: which stored value a donor uses at one allowed-loci combination.</summary>
[Table("DonorSubjectGenotypeSets")]
public class DonorSubjectGenotypeSet
{
    public int Id { get; set; }

    public int DonorId { get; set; }

    public AllowedLociKey AllowedLociKey { get; set; }

    public int SubjectGenotypeSetValueId { get; set; }
}
