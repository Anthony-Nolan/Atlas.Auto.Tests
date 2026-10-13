using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

[Table("DonorManagementLogs")]
public class DonorManagementLog
{
    public int Id { get; set; }

    public int DonorId { get; set; }

    public long SequenceNumberOfLastUpdate { get; set; }

    public DateTimeOffset LastUpdateDateTime { get; set; }
}
