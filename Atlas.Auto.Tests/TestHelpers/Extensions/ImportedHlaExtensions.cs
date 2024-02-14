using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Extensions
{
    internal static class ImportedHlaExtensions
    {
        public static PhenotypeInfoTransfer<string> ToPhenotypeInfoTransfer(this ImportedHla importedHla)
        {
            return new PhenotypeInfoTransfer<string>
            {
                A = importedHla.A.ToLocusInfoTransfer(),
                B = importedHla.B.ToLocusInfoTransfer(),
                C = importedHla.C.ToLocusInfoTransfer(),
                Dpb1 = importedHla.DPB1.ToLocusInfoTransfer(),
                Dqb1 = importedHla.DQB1.ToLocusInfoTransfer(),
                Drb1 = importedHla.DRB1.ToLocusInfoTransfer()
            };
        }

        public static LocusInfoTransfer<string> ToLocusInfoTransfer(this ImportedLocus importedLocus)
        {
            return new LocusInfoTransfer<string>
            {
                Position1 = string.IsNullOrEmpty(importedLocus.Dna.Field1)
                    ? importedLocus.Serology.Field1
                    : importedLocus.Dna.Field1,
                Position2 = string.IsNullOrEmpty(importedLocus.Dna.Field2)
                    ? importedLocus.Serology.Field2
                    : importedLocus.Dna.Field2
            };
        }
    }
}
