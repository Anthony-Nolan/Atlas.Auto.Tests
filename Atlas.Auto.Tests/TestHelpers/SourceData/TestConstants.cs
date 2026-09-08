using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.SourceData
{
    /// <summary>
    /// Central location for set of constants used across tests.
    /// </summary>
    internal class TestConstants
    {
        public const string AutoTestTag = "AutoTest";
        public const string DonorImportTestTag = "DonorImport";
        public const string SearchTestTag = "Search";
        public const string ScoringTestTag = "Scoring";
        public const string RepeatSearchTestTag = "RepeatSearch";

        public const string RecordIdPrefix = AutoTestTag + "Donor";
        public const string DefaultRegistryCode = AutoTestTag + "Registry";
        public const string DefaultEthnicity = AutoTestTag + "Ethnicity";

        public const ImportDonorType DefaultDonorType = ImportDonorType.Adult;
    }
}
