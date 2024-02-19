using Atlas.Auto.Tests.TestHelpers.Services;
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

        public static Builder<DonorUpdate> WithValidDnaAtAllLoci(this Builder<DonorUpdate> builder) =>
            builder.WithFactory(d => d.Hla, ImportedHlaBuilder.ValidHlaAtAllLoci.Build);

        public static Builder<DonorUpdate> WithAlternativeDnaAtLocusA(this Builder<DonorUpdate> builder) =>
            builder.WithFactory(d => d.Hla, ImportedHlaBuilder.ValidHlaAtAllLoci.WithAlternativeHlaAtLocusA().Build);

        public static Builder<DonorUpdate> WithChangeType(this Builder<DonorUpdate> builder, ImportDonorChangeType changeType) =>
            builder.With(d => d.ChangeType, changeType);

        public static Builder<DonorUpdate> WithChangeTypes(this Builder<DonorUpdate> builder, IEnumerable<ImportDonorChangeType> changeTypes) =>
            builder.WithSequentialFrom(d => d.ChangeType, changeTypes);

        public static Builder<DonorUpdate> WithRecordIds(this Builder<DonorUpdate> builder, IEnumerable<string> recordIds) =>
            builder.WithSequentialFrom(d => d.RecordId, recordIds);
    }
}