using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.Search;

/// <summary>
/// Tests that cover happy paths of Atlas search.
/// </summary>
[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
[Category($"{nameof(Search_HappyPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class Search_HappyPathTests : SearchTestBase
{
    public Search_HappyPathTests() : base(nameof(Search_HappyPathTests))
    {
    }

    [Test]
    public async Task Search_Donor_10_10_CompletesSuccessfully()
    {
        const string action = "10/10 Donor Search";
        var test = GetTestServices(nameof(Search_Donor_10_10_CompletesSuccessfully));

        test.Logger.LogStart(action);
        var donorId = await test.Steps.CreateTestDonor(ImportDonorType.Adult);
        var searchResponse = await test.Steps.Submit10Of10DonorSearchRequest();

        test.Logger.LogCompletion(action);
    }
}