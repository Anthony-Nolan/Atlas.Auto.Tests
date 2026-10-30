using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

/// <summary>Link row: which stored value a donor uses at one allowed-loci combination.</summary>
[Table("DonorSubjectGenotypeSets")]
public class DonorSubjectGenotypeSet
{
    public int Id { get; set; }

    public int DonorId { get; set; }

    public AllowedLociKey AllowedLociKey { get; set; }

    public int SubjectGenotypeSetValueId { get; set; }
}
