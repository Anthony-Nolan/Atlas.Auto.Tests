namespace Atlas.Auto.Tests.TestHelpers.Data.Entities;

internal interface IDonorEntity
{
    string ExternalDonorCode { get; }
    string? RegistryCode { get; }
    string? EthnicityCode { get; }
    string? A_1 { get; }
    string? A_2 { get; }
    string? B_1 { get; }
    string? B_2 { get; }
    string? C_1 { get; }
    string? C_2 { get; }
    string? DPB1_1 { get; }
    string? DPB1_2 { get; }
    string? DQB1_1 { get; }
    string? DQB1_2 { get; }
    string? DRB1_1 { get; }
    string? DRB1_2 { get; }
    string DonorTypeName { get; }
}
