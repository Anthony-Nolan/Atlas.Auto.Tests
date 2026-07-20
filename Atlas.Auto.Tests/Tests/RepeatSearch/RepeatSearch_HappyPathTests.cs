using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.DonorImport.FileSchema.Models;
using System.Runtime.CompilerServices;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

[TestFixture]
internal class RepeatSearch_HappyPathTests : RepeatSearchTestBase
{
    private const string TestCategoryPrefix = nameof(RepeatSearch_HappyPathTests);

    public RepeatSearch_HappyPathTests() : base(TestCategoryPrefix)
    {
    }

    [Category($"{TestCategoryPrefix}_Adult")]
    [TestCaseSource(nameof(Cases))]
    public async Task RepeatSearch_Donor_10_10_IdentifiedExpectedChanges(bool? parallelMatchPrediction)
    {
        await RunRepeatSearch(
            testDescription: "Repeat Search tests for 10/10 donor search",
            donorType: ImportDonorType.Adult,
            requestFileName: "search-request-donor-10_10.json",
            parallelMatchPrediction: parallelMatchPrediction);
    }

    [Category($"{TestCategoryPrefix}_Cord")]
    [TestCaseSource(nameof(Cases))]
    public async Task RepeatSearch_Cord_4_8_IdentifiedExpectedChanges(bool? parallelMatchPrediction)
    {
        await RunRepeatSearch(
            testDescription: "Repeat Search tests for 4/8 cord search",
            donorType: ImportDonorType.Cord,
            requestFileName: "search-request-cord-4_8.json",
            parallelMatchPrediction: parallelMatchPrediction);
    }

    private async Task RunRepeatSearch(
        string testDescription,
        ImportDonorType donorType,
        string requestFileName,
        bool? parallelMatchPrediction,
        [CallerMemberName] string callerName = "")
    {
        await ExecuteWithRetry(async () =>
        {
            var test = GetRepeatSearchTestServices(callerName);
            test.Logger.LogStart(testDescription);
            await RunRepeatSearchTests(test, donorType, requestFileName, parallelMatchPrediction);
            test.Logger.LogCompletion(testDescription);
        });
    }

    private static async Task RunRepeatSearchTests(
        TestServices<IRepeatSearchTestSteps> test,
        ImportDonorType donorType,
        string requestFileName,
        bool? parallelMatchPrediction)
    {
        var currentTestStep = "Create donors then run original search";
        test.Logger.LogStart(currentTestStep);
        var firstDonors = await CreateFirstDonors(test, donorType);
        var searchId = await test.Steps.OriginalSearchShouldOnlyReturnExpectedDonors(requestFileName, firstDonors, parallelMatchPrediction);
        test.Logger.LogCompletion(currentTestStep);

        currentTestStep = "Apply donor updates then run repeat search";
        test.Logger.LogStart(currentTestStep);
        var timeBeforeDonorChanges = DateTimeOffset.UtcNow;
        var donorChanges = await ApplyDonorChanges(test, donorType, firstDonors);
        await RepeatSearchShouldIdentifyExpectedChanges(test, requestFileName, searchId, timeBeforeDonorChanges, donorChanges, parallelMatchPrediction);
        test.Logger.LogCompletion(currentTestStep);

        currentTestStep = "Delete previously matched donors then run repeat search";
        test.Logger.LogStart(currentTestStep);
        timeBeforeDonorChanges = DateTimeOffset.UtcNow;
        donorChanges = await DeleteDonors(test, donorChanges.NewlyMatching);
        await RepeatSearchShouldIdentifyExpectedChanges(test, requestFileName, searchId, timeBeforeDonorChanges, donorChanges, parallelMatchPrediction);
        test.Logger.LogCompletion(currentTestStep);
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
        DonorChanges donorChanges,
        bool? parallelMatchPrediction)
    {
        var repeatRunId = await test.Steps.SubmitRepeatSearchRequest(searchRequestFileName, originalSearchId, searchCutOff, parallelMatchPrediction);
        await test.Steps.RepeatMatchingShouldHaveIdentifiedExpectedChanges(repeatRunId, originalSearchId, donorChanges);
        await test.Steps.RepeatSearchShouldHaveIdentifiedExpectedChanges(repeatRunId, originalSearchId, donorChanges);
    }
}
