using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

[Table("DonorImportFailures", Schema = "Donors")]
public class DonorImportFailure
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string? ExternalDonorCode { get; set; }

    [MaxLength(64)]
    public string? DonorType { get; set; }

    [MaxLength(256)]
    public string? EthnicityCode { get; set; }

    [MaxLength(256)]
    public string? RegistryCode { get; set; }

    public string? UpdateFile { get; set; }

    [MaxLength(256)]
    public string? UpdateProperty { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset FailureTime { get; set; }
}
