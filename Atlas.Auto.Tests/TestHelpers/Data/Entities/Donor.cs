using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

public enum DatabaseDonorType { Adult = 0, Cord = 1 }

[Table("Donors", Schema = "Donors")]
[Index(nameof(ExternalDonorCode), IsUnique = true)]
public class Donor : IDonorEntity
{
    public string DonorTypeName => DonorType.ToString();
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AtlasId { get; set; }

    [MaxLength(64)]
    public string ExternalDonorCode { get; set; } = null!;

    [MaxLength(256)]
    public string? UpdateFile { get; set; }

    public DateTimeOffset LastUpdated { get; set; }

    public DatabaseDonorType DonorType { get; set; }

    [MaxLength(256)]
    public string? EthnicityCode { get; set; }

    [MaxLength(256)]
    public string? RegistryCode { get; set; }

    public string? A_1 { get; set; }
    public string? A_2 { get; set; }
    public string? B_1 { get; set; }
    public string? B_2 { get; set; }
    public string? C_1 { get; set; }
    public string? C_2 { get; set; }
    public string? DPB1_1 { get; set; }
    public string? DPB1_2 { get; set; }
    public string? DQB1_1 { get; set; }
    public string? DQB1_2 { get; set; }
    public string? DRB1_1 { get; set; }
    public string? DRB1_2 { get; set; }

    public string? Hash { get; set; }
}
