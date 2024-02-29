using Atlas.Auto.Tests.TestHelpers.SourceData;

namespace Atlas.Auto.Tests.TestHelpers.Builders
{
    internal static class DonorImportGenerators
    {
        public static Func<string> RecordIdFactory()
        {
            return () => $"{TestConstants.RecordIdPrefix}-{Guid.NewGuid()}";
        }

        public static string BuildFileName()
        {
            return $"{TestConstants.AutoTestTag}_{Guid.NewGuid()}.json";
        }
    }
}