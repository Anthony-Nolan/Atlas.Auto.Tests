using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.SourceData
{
    /// <summary>
    /// Central location for set of constants used across tests.
    /// </summary>
    internal class TestConstants
    {
        public const string AutoTestTag = "AutoTest";

        public static string RecordIdPrefix => $"{AutoTestTag}Donor";
        public static string DefaultRegistryCode => $"{AutoTestTag}Registry";
        public static string DefaultEthnicity => $"{AutoTestTag}Ethnicity";

        public static ImportDonorType DefaultDonorType => ImportDonorType.Adult;
    }
}
