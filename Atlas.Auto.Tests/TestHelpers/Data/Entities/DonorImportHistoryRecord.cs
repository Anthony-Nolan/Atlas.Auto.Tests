using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

[Table("DonorImportHistory", Schema = "Donors")]
public class DonorImportHistoryRecord
{
    public const string StartedState = "Started";
    public const string CompletedState = "Completed";

    public int Id { get; set; }

    public string Filename { get; set; } = null!;

    public DateTime UploadTime { get; set; }

    public string FileState { get; set; } = null!;

    public DateTime? ImportEnd { get; set; }

    public int FailureCount { get; set; }

    public int ImportedDonorsCount { get; set; }

    public int? FailedDonorCount { get; set; }
}
