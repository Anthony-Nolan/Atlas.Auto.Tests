namespace Atlas.Auto.Tests.TestHelpers.SourceData
{
    /// <summary>
    /// HLA typings that can be used to build test patients and donors.
    /// </summary>
    internal static class HlaTypings
    {
        #region Valid typings
        // A set of HLA typings that are relatively old and are unlikely to be deleted in future versions of the IMGT/HLA database.
        public const string ValidDnaForLocusA = "*01:01";
        public const string ValidDnaForLocusB = "*07:02";
        public const string ValidDnaForLocusC = "*01:02";
        public const string ValidDnaForLocusDpb1 = "*03:01";
        public const string ValidDnaForLocusDqb1 = "*02:01";
        public const string ValidDnaForLocusDrb1 = "*04:01";

        // Use for testing donor edits
        public const string AlternativeValidDnaForLocusA = "*11:11";
        #endregion

        // This typing fits the pattern of a valid MAC but does not actually exist.
        public const string InvalidDnaForAnyLocus = "*999:INVALIDHLA";
    }
}
