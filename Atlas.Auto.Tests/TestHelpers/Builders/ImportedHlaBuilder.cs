using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

namespace Atlas.Auto.Tests.TestHelpers.Builders
{
    internal static class ImportedHlaBuilder
    {
        public static Builder<ImportedHla> ValidDnaPhenotype => BuildFromPhenotype(HlaTypings.ValidDnaPhenotype);

        public static Builder<ImportedHla> InvalidHlaAtAllLoci => BuildWithSameHlaAtAllLoci(HlaTypings.InvalidDnaForAnyLocus);

        public static Builder<ImportedHla> SearchTestPhenotype => BuildFromPhenotype(HlaTypings.SearchTestPhenotype);

        public static Builder<ImportedHla> SearchNewPhenotype => BuildFromPhenotype(HlaTypings.SearchNewPhenotype);

        public static Builder<ImportedHla> AssociatedAntigenPhenotype => BuildFromPhenotype(HlaTypings.SearchAssociatedPhenotype);

        public static Builder<ImportedHla> WithAlternativeHlaAtLocusA(this Builder<ImportedHla> builder)
            => builder.WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.AlternativeValidDnaForLocusA));

        public static Builder<ImportedHla> WithNoHlaAtDrb1(this Builder<ImportedHla> builder) => builder.WithNew(h => h.DRB1);

        private static Builder<ImportedHla> BuildFromPhenotype(PhenotypeInfoTransfer<string> phenotype) =>
            Builder<ImportedHla>.New
                .WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(phenotype.A))
                .WithFactory(h => h.B, () => ImportedLocusBuilder.BuildLocusWithDna(phenotype.B))
                .WithFactory(h => h.C, () => ImportedLocusBuilder.BuildLocusWithDna(phenotype.C))
                .WithFactory(h => h.DPB1, () => ImportedLocusBuilder.BuildLocusWithDna(phenotype.Dpb1))
                .WithFactory(h => h.DQB1, () => ImportedLocusBuilder.BuildLocusWithDna(phenotype.Dqb1))
                .WithFactory(h => h.DRB1, () => ImportedLocusBuilder.BuildLocusWithDna(phenotype.Drb1));

        private static Builder<ImportedHla> BuildWithSameHlaAtAllLoci(string dna) =>
            Builder<ImportedHla>.New
                .WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(dna))
                .WithFactory(h => h.B, () => ImportedLocusBuilder.BuildLocusWithDna(dna))
                .WithFactory(h => h.C, () => ImportedLocusBuilder.BuildLocusWithDna(dna))
                .WithFactory(h => h.DPB1, () => ImportedLocusBuilder.BuildLocusWithDna(dna))
                .WithFactory(h => h.DQB1, () => ImportedLocusBuilder.BuildLocusWithDna(dna))
                .WithFactory(h => h.DRB1, () => ImportedLocusBuilder.BuildLocusWithDna(dna));
    }
}
