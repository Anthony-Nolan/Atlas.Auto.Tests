using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

public enum MatchingDonorType { Adult = 1, Cord = 2 }

[Table("Donors")]
[Index(nameof(DonorId))]
[Index(nameof(ExternalDonorCode))]
public class MatchingAlgorithmDonor : IDonorEntity
{
    public string DonorTypeName => DonorType.ToString();
    [Key]
    public int Id { get; set; }

    public int DonorId { get; set; }

    public MatchingDonorType DonorType { get; set; }

    public bool IsAvailableForSearch { get; set; }

    [Required]
    [MaxLength(64)]
    public string ExternalDonorCode { get; set; } = null!;

    [MaxLength(256)]
    public string? EthnicityCode { get; set; }

    [MaxLength(256)]
    public string? RegistryCode { get; set; }

    [Required]
    public string A_1 { get; set; } = null!;

    [Required]
    public string A_2 { get; set; } = null!;

    [Required]
    public string B_1 { get; set; } = null!;

    [Required]
    public string B_2 { get; set; } = null!;

    public string? C_1 { get; set; }
    public string? C_2 { get; set; }
    public string? DPB1_1 { get; set; }
    public string? DPB1_2 { get; set; }
    public string? DQB1_1 { get; set; }
    public string? DQB1_2 { get; set; }

    [Required]
    public string DRB1_1 { get; set; } = null!;

    [Required]
    public string DRB1_2 { get; set; } = null!;
}
