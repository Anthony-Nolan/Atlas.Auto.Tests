using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

[Table("PublishableDonorUpdates", Schema = "Donors")]
public class PublishableDonorUpdate
{
    public int Id { get; set; }

    public int DonorId { get; set; }

    public string SearchableDonorUpdate { get; set; } = null!;

    public DateTimeOffset CreatedOn { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishedOn { get; set; }
}
