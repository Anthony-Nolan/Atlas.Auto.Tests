using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Auto.Tests.TestHelpers.Extensions;

internal static class DonorEntityExtensions
{
    public static PhenotypeInfoTransfer<string> GetHla(this IDonorEntity donor)
    {
        return new PhenotypeInfo<string>(
                valueA_1: donor.A_1 ?? string.Empty, valueA_2: donor.A_2 ?? string.Empty,
                valueB_1: donor.B_1 ?? string.Empty, valueB_2: donor.B_2 ?? string.Empty,
                valueC_1: donor.C_1 ?? string.Empty, valueC_2: donor.C_2 ?? string.Empty,
                valueDpb1_1: donor.DPB1_1 ?? string.Empty, valueDpb1_2: donor.DPB1_2 ?? string.Empty,
                valueDqb1_1: donor.DQB1_1 ?? string.Empty, valueDqb1_2: donor.DQB1_2 ?? string.Empty,
                valueDrb1_1: donor.DRB1_1 ?? string.Empty, valueDrb1_2: donor.DRB1_2 ?? string.Empty)
            .ToPhenotypeInfoTransfer();
    }
}
