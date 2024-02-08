using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Builders
{
    internal static class ImportedLocusBuilder
    {
        public static ImportedLocus BuildLocusWithDna(string dna) => new() { Dna = BuildTwoField(dna) };

        private static TwoFieldStringData BuildTwoField(string hla) => new() { Field1 = hla, Field2 = hla };
    }
}
