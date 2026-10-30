using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Auto.Tests.TestHelpers.SourceData
{
    /// <summary>
    /// HLA typings that can be used to build test patients and donors.
    /// Note: <see cref="ValidDnaPhenotype"/> should not match <see cref="SearchTestPhenotype"/>, either 10/10 or 4/8.
    /// </summary>
    internal static class HlaTypings
    {
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
        /// Phenotype constructed from valid molecular HLA typings.
        /// Intended for use in testing donor import.
        /// Use <see cref="SearchTestPhenotype"/> when testing search.
        /// </summary>
        public static readonly PhenotypeInfoTransfer<string> ValidDnaPhenotype = new PhenotypeInfo<string>(
            ValidDnaForLocusA, ValidDnaForLocusA,
            ValidDnaForLocusB, ValidDnaForLocusB,
            ValidDnaForLocusC, ValidDnaForLocusC,
            ValidDnaForLocusDpb1, ValidDnaForLocusDpb1,
            ValidDnaForLocusDqb1, ValidDnaForLocusDqb1,
            ValidDnaForLocusDrb1, ValidDnaForLocusDrb1).ToPhenotypeInfoTransfer();

        /// <summary>
        /// Use for testing donor edits
        /// </summary>
        public const string AlternativeValidDnaForLocusA = "*11:11";

        private static readonly PhenotypeInfo<string> ExpandedHla = new(
            "*01:DXGWF", "*01:DXGWF",
            "*07:DXFTK", "*07:DXFTK",
            "*05:DUVRN", "*07:BRXNC",
            "*03:FYKD", "*04:BYVXE",
            "*03:BMSUA", "*06:BSBZX",
            "*15:01:01", "*12:JV");

        /// <summary>
        /// Phenotype mostly typed with NMDP codes, so it has to be expanded on import.
        /// Taken from QA-1926 test data.
        /// </summary>
        public static readonly PhenotypeInfoTransfer<string> ExpandedHlaPhenotype = ExpandedHla.ToPhenotypeInfoTransfer();

        /// <summary>
        /// <see cref="ExpandedHlaPhenotype"/> with a different typing at A position 2; use for testing donor edits.
        /// </summary>
        public static readonly PhenotypeInfoTransfer<string> ExpandedHlaPhenotypeWithAlternativeA2 =
            ExpandedHla.SetPosition(Locus.A, LocusPosition.Two, "*11:01").ToPhenotypeInfoTransfer();

        /// <summary>
        /// This typing fits the pattern of a valid MAC but does not actually exist.
        /// </summary>
        public const string InvalidDnaForAnyLocus = "*999:INVALIDHLA";

        /// <summary>
        /// Phenotype constructed from two A~B~C~Q~R haplotypes within the test HF set to ensure it is deemed "represented" during match prediction.
        /// The haplotypes themselves are relatively rare in an effort to minimise the number of matches found during search.
        /// The typing has purposely been made ambiguous at loci A, B and DRB1 to encourage "genotype expansion" during match prediction.
        /// The original high resolution phenotype is available as the patient HLA within the saved search requests.
        /// </summary>
        public static readonly PhenotypeInfoTransfer<string> SearchTestPhenotype = new PhenotypeInfo<string>(
            "*31:XX", "*32:XX", 
            "*14:XX", "*35:XX", 
            "*01:02", "*08:180N", 
            "*01:01", "*11:01",
            "*02:01", "*03:02",
            "*03:XX", "*09:XX")
            .ToPhenotypeInfoTransfer();

        /// <summary>
        /// Two haplotypes of the autotest frequency set that no other typing here uses, so a search for it finds only
        /// the donor of the precomputed genotype set test, and its usage event counts that donor alone. Ambiguous, as
        /// Atlas stores no precomputed genotype set for an unambiguous typing.
        /// </summary>
        public static readonly PhenotypeInfoTransfer<string> PrecomputeTestPhenotype = new PhenotypeInfo<string>(
            "*30:XX", "*68:XX",
            "*15:XX", "*48:XX",
            "*04:01", "*03:03",
            "*01:01", "*11:01",
            "*06:02", "*04:02",
            "*11:XX", "*08:XX")
            .ToPhenotypeInfoTransfer();

        public static readonly PhenotypeInfoTransfer<string> SearchNewPhenotype = new PhenotypeInfo<string>(
            "NEW", "*32:XX",
            "*14:XX", "*35:XX",
            "*01:02", "*08:180N",
            "*01:01", "*11:01",
            "*02:01", "*03:02",
            "*03:XX", "*09:XX")
            .ToPhenotypeInfoTransfer();

        public static readonly PhenotypeInfoTransfer<string> SearchAssociatedPhenotype = new PhenotypeInfo<string>(
            "*0265", "*33:01",
            "*14:02", "*35:01",
            "*06:02", "*12",
            "*01:01", "*11:01",
            "*02:01", "*03:01",
            "*07:01", "*1201")
            .ToPhenotypeInfoTransfer();
    }
}