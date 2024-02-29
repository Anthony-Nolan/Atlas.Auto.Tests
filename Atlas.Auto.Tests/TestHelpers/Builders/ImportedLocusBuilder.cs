using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Builders
{
    internal static class ImportedLocusBuilder
    {
        public static ImportedLocus BuildLocusWithDna(string dna) => new() { Dna = BuildTwoField(dna) };

        public static ImportedLocus BuildLocusWithDna(LocusInfoTransfer<string> dna) =>
            new() { Dna = BuildTwoField(dna.Position1, dna.Position2) };

        private static TwoFieldStringData BuildTwoField(string hla, string? optionalHla2 = null) =>
            new() { Field1 = hla, Field2 = optionalHla2 ?? hla };
    }
}
