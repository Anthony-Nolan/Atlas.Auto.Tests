using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

namespace Atlas.Auto.Tests.TestHelpers.Builders
{
    internal static class ImportedHlaBuilder
    {
        public static Builder<ImportedHla> ValidHlaAtAllLoci => Builder<ImportedHla>.New
            .WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaPhenotype.A))
            .WithFactory(h => h.B, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaPhenotype.B))
            .WithFactory(h => h.C, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaPhenotype.C))
            .WithFactory(h => h.DPB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaPhenotype.Dpb1))
            .WithFactory(h => h.DQB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaPhenotype.Dqb1))
            .WithFactory(h => h.DRB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaPhenotype.Drb1));

        public static Builder<ImportedHla> InvalidHlaAtAllLoci => Builder<ImportedHla>.New
            .WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.B, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.C, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.DPB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.DQB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.DRB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus));

        public static Builder<ImportedHla> SearchTestPhenotype => Builder<ImportedHla>.New
            .WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.SearchTestPhenotype.A))
            .WithFactory(h => h.B, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.SearchTestPhenotype.B))
            .WithFactory(h => h.C, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.SearchTestPhenotype.C))
            .WithFactory(h => h.DPB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.SearchTestPhenotype.Dpb1))
            .WithFactory(h => h.DQB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.SearchTestPhenotype.Dqb1))
            .WithFactory(h => h.DRB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.SearchTestPhenotype.Drb1));

        public static Builder<ImportedHla> WithAlternativeHlaAtLocusA(this Builder<ImportedHla> builder) 
            => builder.WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.AlternativeValidDnaForLocusA));

        public static Builder<ImportedHla> WithNoHlaAtDrb1(this Builder<ImportedHla> builder) => builder.WithNew(h => h.DRB1);
    }
}