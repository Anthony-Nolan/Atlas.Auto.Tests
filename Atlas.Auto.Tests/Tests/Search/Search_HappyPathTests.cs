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
        const string testName = nameof(Search_Donor_10_10_ReturnsExpectedSearchResult);
        var test = GetTestServices(testName);

        const string testDescription = "10/10 Donor Search";
        test.Logger.LogStart(testDescription);

        var expectedDonorCode = await test.Steps.CreateTestDonor(ImportDonorType.Adult);
        var searchResponse = await test.Steps.Submit10Of10DonorSearchRequest();
        await test.Steps.MatchingShouldHaveBeenSuccessful(searchResponse.SearchIdentifier, expectedDonorCode, testName);

        test.Logger.LogCompletion(testDescription);
    }
}