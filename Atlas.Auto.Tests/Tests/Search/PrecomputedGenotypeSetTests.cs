using Atlas.Auto.Tests.TestHelpers.Attributes;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.Search;

/// <summary>
/// Needs MATCH_PREDICTION_PRECOMPUTE_MODE = DefaultLive or DefaultPrecomputed on the target Atlas: with ForceLive the
/// search ignores the request's UsePrecomputedGenotypeSets, so the usage check only warns.
/// </summary>
[TestFixture]
[Category($"{TestConstants.SearchTestTag}_{nameof(PrecomputedGenotypeSetTests)}")]
// Runs in the SearchHappyPathTests pipeline job: not worth an agent of its own
[Category(nameof(SearchHappyPathTests))]
internal class PrecomputedGenotypeSetTests : SearchTestBase
{
    public PrecomputedGenotypeSetTests() : base(nameof(PrecomputedGenotypeSetTests))
    {
    }

    /// <summary>
    /// An imported donor gets a precomputed genotype set per allowed-loci combination, a search reads them and gets the
    /// same result as the live 10/10 search, and an HLA edit replaces them with sets for the new typing.
    /// </summary>
    [Test]
    [ManualTest("QA-2881")]
    [RequiresAppInsights]
    public async Task Search_Donor_10_10_UsesPrecomputedGenotypeSets()
    {
        var steps = GetSearchTestSteps(nameof(Search_Donor_10_10_UsesPrecomputedGenotypeSets));
        var precomputeSteps = new PrecomputedGenotypeSetTestSteps(Provider, steps.Logger);

        // Create
        var donorCode = await steps.CreateDonor(ImportDonorType.Adult, ImportedHlaBuilder.SearchTestPhenotype);
        var donorId = await precomputeSteps.GetMatchingDonorId(donorCode);
        var createdSets = await precomputeSteps.DonorShouldHavePrecomputedGenotypeSets(donorId);

        // Search
        var searchResponse = await steps.SubmitSearchRequest("search-request-donor-10_10.json", usePrecomputedGenotypeSets: true);
        await steps.MatchingShouldReturnExpectedDonor(searchResponse.SearchIdentifier, donorCode);
        await steps.SearchShouldReturnExpectedDonor(searchResponse.SearchIdentifier, donorCode);
        await precomputeSteps.PrecomputedGenotypeSetsShouldHaveBeenUsed(searchResponse.SearchIdentifier);

        // Edit
        await steps.DonorImportSteps.EditDonorHla(donorCode, ImportDonorType.Adult, ImportedHlaBuilder.SearchTestPhenotype.WithAlternativeHlaAtLocusA());
        await precomputeSteps.DonorShouldHavePrecomputedGenotypeSets(donorId, createdSets);
    }
}
