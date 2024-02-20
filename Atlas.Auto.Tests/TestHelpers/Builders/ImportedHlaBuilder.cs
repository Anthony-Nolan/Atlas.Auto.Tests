using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

namespace Atlas.Auto.Tests.TestHelpers.Builders
{
    internal static class ImportedHlaBuilder
    {
        public static Builder<ImportedHla> ValidHlaAtAllLoci => Builder<ImportedHla>.New
            .WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaForLocusA))
            .WithFactory(h => h.B, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaForLocusB))
            .WithFactory(h => h.C, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaForLocusC))
            .WithFactory(h => h.DPB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaForLocusDpb1))
            .WithFactory(h => h.DQB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaForLocusDqb1))
            .WithFactory(h => h.DRB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.ValidDnaForLocusDrb1));

        public static Builder<ImportedHla> InvalidHlaAtAllLoci => Builder<ImportedHla>.New
            .WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.B, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.C, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.DPB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.DQB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus))
            .WithFactory(h => h.DRB1, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.InvalidDnaForAnyLocus));

        public static Builder<ImportedHla> WithAlternativeHlaAtLocusA(this Builder<ImportedHla> builder) 
            => builder.WithFactory(h => h.A, () => ImportedLocusBuilder.BuildLocusWithDna(HlaTypings.AlternativeValidDnaForLocusA));

        public static Builder<ImportedHla> WithNoHlaAtDrb1(this Builder<ImportedHla> builder) => builder.WithNew(h => h.DRB1);
    }
}