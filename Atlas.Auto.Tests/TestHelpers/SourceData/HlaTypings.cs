using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Auto.Tests.TestHelpers.SourceData
{
    /// <summary>
    /// HLA typings that can be used to build test patients and donors.
    /// </summary>
    internal static class HlaTypings
    {
        #region Valid typings
        /// <summary>
        /// A set of HLA typings that are relatively old and are unlikely to be deleted in future versions of the IMGT/HLA database.
        /// </summary>
        public const string ValidDnaForLocusA = "*01:01";
        public const string ValidDnaForLocusB = "*07:02";
        public const string ValidDnaForLocusC = "*01:02";
        public const string ValidDnaForLocusDpb1 = "*03:01";
        public const string ValidDnaForLocusDqb1 = "*02:01";
        public const string ValidDnaForLocusDrb1 = "*04:01";

        /// <summary>
        /// Use for testing donor edits
        /// </summary>
        public const string AlternativeValidDnaForLocusA = "*11:11";
        #endregion

        /// <summary>
        /// This typing fits the pattern of a valid MAC but does not actually exist.
        /// </summary>
        public const string InvalidDnaForAnyLocus = "*999:INVALIDHLA";

        /// <summary>
        /// Phenotype constructed from two A~B~C~Q~R haplotypes within the test HF set to ensure it is deemed "represented" during match prediction.
        /// The haplotypes themselves are relatively rare in an effort to minimise the number of matches found during search.
        /// </summary>
        public static PhenotypeInfoTransfer<string> TestHlaPhenotypeForSearch = new PhenotypeInfo<string>(
            "*31:01", "*32:01", 
            "*14:01", "*35:01", 
            "*01:02", "*08:180N", 
            "*01:01", "*11:01",
            "*02:01", "*03:02",
            "*03:01", "*09:01")
            .ToPhenotypeInfoTransfer();
    }
}
