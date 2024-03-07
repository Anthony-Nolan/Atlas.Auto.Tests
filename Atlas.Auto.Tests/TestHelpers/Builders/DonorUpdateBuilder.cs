using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

namespace Atlas.Auto.Tests.TestHelpers.Builders
{
    internal static class DonorUpdateBuilder
    {
        public static Builder<DonorUpdate> New => Builder<DonorUpdate>.New
            .WithFactory(d => d.RecordId, DonorImportGenerators.RecordIdFactory());

        /// <summary>
        /// Builder with default values set at some fields.
        /// HLA and ChangeType left unset.
        /// </summary>
        public static Builder<DonorUpdate> Default => New
            .With(d => d.DonorType, TestConstants.DefaultDonorType)
            .With(d => d.RegistryCode, TestConstants.DefaultRegistryCode)
            .With(d => d.Ethnicity, TestConstants.DefaultEthnicity);

        public static Builder<DonorUpdate> WithValidDnaPhenotype(this Builder<DonorUpdate> builder) =>
            builder.WithHla(ImportedHlaBuilder.ValidDnaPhenotype);

        public static Builder<DonorUpdate> WithAlternativeDnaAtLocusA(this Builder<DonorUpdate> builder) =>
            builder.WithHla(ImportedHlaBuilder.ValidDnaPhenotype.WithAlternativeHlaAtLocusA());

        public static Builder<DonorUpdate> WithInvalidDnaAtAllLoci(this Builder<DonorUpdate> builder) =>
            builder.WithHla(ImportedHlaBuilder.InvalidHlaAtAllLoci);

        public static Builder<DonorUpdate> WithHlaAtEveryLocusExceptDrb1(this Builder<DonorUpdate> builder) =>
            builder.WithHla(ImportedHlaBuilder.ValidDnaPhenotype.WithNoHlaAtDrb1());

        public static Builder<DonorUpdate> WithSearchTestPhenotype(this Builder<DonorUpdate> builder) =>
            builder.WithHla(ImportedHlaBuilder.SearchTestPhenotype);

        public static Builder<DonorUpdate> WithHla(this Builder<DonorUpdate> builder, Builder<ImportedHla> hlaBuilder) =>
            builder.WithFactory(d => d.Hla, hlaBuilder.Build);

        public static Builder<DonorUpdate> WithChangeType(this Builder<DonorUpdate> builder, ImportDonorChangeType changeType) =>
            builder.With(d => d.ChangeType, changeType);

        public static Builder<DonorUpdate> WithChangeTypes(this Builder<DonorUpdate> builder, IEnumerable<ImportDonorChangeType> changeTypes) =>
            builder.WithSequentialFrom(d => d.ChangeType, changeTypes);

        public static Builder<DonorUpdate> WithRecordIds(this Builder<DonorUpdate> builder, IEnumerable<string> recordIds) =>
            builder.WithSequentialFrom(d => d.RecordId, recordIds);

        public static Builder<DonorUpdate> WithDonorType(this Builder<DonorUpdate> builder, ImportDonorType donorType) =>
            builder.With(d => d.DonorType, donorType);
    }
}