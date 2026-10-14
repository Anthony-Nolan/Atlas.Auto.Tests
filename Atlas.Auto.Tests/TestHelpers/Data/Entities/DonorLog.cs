using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

[Table("DonorLogs", Schema = "Donors")]
public class DonorLog
{
    [Key]
    public string ExternalDonorCode { get; set; } = null!;

    public DateTime LastUpdateFileUploadTime { get; set; }
}
