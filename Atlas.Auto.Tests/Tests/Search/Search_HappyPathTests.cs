using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.Search;

/// <summary>
/// Tests that cover happy paths of Atlas search.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category($"{nameof(Search_HappyPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class Search_HappyPathTests : SearchTestBase
{
    public Search_HappyPathTests() : base(nameof(Search_HappyPathTests))
    {
    }

    [Test]
    public async Task Search_Donor_10_10_ReturnsExpectedSearchResult()
    {
        var test = GetTestServices(nameof(Search_Donor_10_10_ReturnsExpectedSearchResult));

        const string testDescription = "10/10 Donor Search";
        test.Logger.LogStart(testDescription);

        var expectedDonorCode = await test.Steps.CreateTestDonor(ImportDonorType.Adult);
        var searchResponse = await test.Steps.SubmitSearchRequest("search-request-donor-10_10.json");
        await test.Steps.MatchingShouldHaveReturnedExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);
        await test.Steps.SearchShouldHaveReturnedExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);

        test.Logger.LogCompletion(testDescription);
    }

    [Test]
    public async Task Search_Cord_4_8_ReturnsExpectedSearchResult()
    {
        var test = GetTestServices(nameof(Search_Cord_4_8_ReturnsExpectedSearchResult));

        const string testDescription = "4/8 Cord Search";
        test.Logger.LogStart(testDescription);

        var expectedDonorCode = await test.Steps.CreateTestDonor(ImportDonorType.Cord);
        var searchResponse = await test.Steps.SubmitSearchRequest("search-request-cord-4_8.json");
        await test.Steps.MatchingShouldHaveReturnedExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);
        await test.Steps.SearchShouldHaveReturnedExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);

        test.Logger.LogCompletion(testDescription);
    }
}