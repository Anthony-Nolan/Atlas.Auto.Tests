using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

/// <summary>
/// Tests that cover happy paths of Atlas repeat search.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category($"{nameof(RepeatSearch_HappyPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class RepeatSearch_HappyPathTests : RepeatSearchTestBase
{
    public RepeatSearch_HappyPathTests() : base(nameof(RepeatSearch_HappyPathTests))
    {
    }

    [Test]
    public async Task RepeatSearch_Donor_10_10_IdentifiedExpectedChanges()
    {
        const string testName = nameof(RepeatSearch_Donor_10_10_IdentifiedExpectedChanges);
        const ImportDonorType donorType = ImportDonorType.Adult;
        const string requestFileName = "search-request-donor-10_10.json";
        var test = GetRepeatSearchTestServices(testName);

        const string testDescription = "Repeat Search of 10/10 donor search";
        test.Logger.LogStart(testDescription);

        var currentTestStep = "Create donors then run original search";
        test.Logger.LogStart(currentTestStep);
        var firstDonors = await CreateFirstDonors(test, donorType);
        var originalSearchId = await test.Steps.OriginalSearchShouldOnlyReturnExpectedDonors(requestFileName, firstDonors);
        test.Logger.LogCompletion(currentTestStep);

        currentTestStep = "Apply donor updates then run repeat search";
        test.Logger.LogStart(currentTestStep);
        var timeBeforeDonorChanges = DateTimeOffset.UtcNow;
        var donorChanges = await ApplyDonorChanges(test, donorType, firstDonors);
        await RepeatSearchShouldIdentifyExpectedChanges(test, requestFileName, originalSearchId, timeBeforeDonorChanges, donorChanges);

        test.Logger.LogCompletion(currentTestStep);

        currentTestStep = "Delete previously matched donors then run repeat search";
        test.Logger.LogStart(currentTestStep);
        timeBeforeDonorChanges = DateTimeOffset.UtcNow;
        donorChanges = await DeleteDonors(test, donorChanges.NewlyMatching);
        await RepeatSearchShouldIdentifyExpectedChanges(test, requestFileName, originalSearchId, timeBeforeDonorChanges, donorChanges);
        test.Logger.LogCompletion(currentTestStep);

        test.Logger.LogCompletion(testDescription);
    }

    private static async Task<DonorChanges> CreateFirstDonors(
        TestServices<IRepeatSearchTestSteps> test,
        ImportDonorType donorType)
    {
        var nonMatchingDonor = await test.Steps.CreateNonMatchingDonor(donorType);
        var matchingDonor = await test.Steps.CreateMatchingDonor(donorType);
        return new DonorChanges
        {
            NoLongerMatching = new[] { nonMatchingDonor },
            NewlyMatching = new[] { matchingDonor }
        };
    }

    private static async Task<DonorChanges> ApplyDonorChanges(
        TestServices<IRepeatSearchTestSteps> test,
        ImportDonorType donorType,
        DonorChanges originalDonorChanges)
    {
        foreach (var donorCode in originalDonorChanges.NewlyMatching)
        {
            await test.Steps.EditDonorToNoLongerMatch(donorCode, donorType);
        }

        foreach (var donorCode in originalDonorChanges.NoLongerMatching)
        {
            await test.Steps.EditDonorToMatch(donorCode, donorType);
        }

        var newMatchingDonor = await test.Steps.CreateMatchingDonor(donorType);

        return new DonorChanges
        {
            NoLongerMatching = originalDonorChanges.NewlyMatching,
            NewlyMatching = originalDonorChanges.NoLongerMatching.Append(newMatchingDonor).ToList()
        };
    }

    private static async Task<DonorChanges> DeleteDonors(
        TestServices<IRepeatSearchTestSteps> test,
        IReadOnlyCollection<string> donors)
    {
        await test.Steps.DeleteDonors(donors);

        return new DonorChanges
        {
            NoLongerMatching = donors,
            NewlyMatching = new List<string>()
        };
    }

    private static async Task RepeatSearchShouldIdentifyExpectedChanges(
        TestServices<IRepeatSearchTestSteps> test,
        string searchRequestFileName,
        string originalSearchId,
        DateTimeOffset searchCutOff,
        DonorChanges donorChanges)
    {
        var repeatRunId = await test.Steps.SubmitRepeatSearchRequest(searchRequestFileName, originalSearchId, searchCutOff);
        await test.Steps.RepeatMatchingShouldHaveIdentifiedExpectedChanges(repeatRunId, originalSearchId, donorChanges);
        await test.Steps.RepeatSearchShouldHaveIdentifiedExpectedChanges(repeatRunId, originalSearchId, donorChanges);
    }
}