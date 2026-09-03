using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.DonorImport.FileSchema.Models;
using System.Runtime.CompilerServices;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

[TestFixture]
internal class RepeatSearchHappyPathTests : RepeatSearchTestBase
{
    private const string TestCategoryPrefix = nameof(RepeatSearchHappyPathTests);

    public RepeatSearchHappyPathTests() : base(TestCategoryPrefix)
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
            var steps = GetRepeatSearchTestSteps(callerName);
            steps.Logger.LogStart(testDescription);
            await RunRepeatSearchTests(steps, donorType, requestFileName, parallelMatchPrediction);
            steps.Logger.LogCompletion(testDescription);
        });
    }

    private static async Task RunRepeatSearchTests(
        RepeatSearchTestSteps steps,
        ImportDonorType donorType,
        string requestFileName,
        bool? parallelMatchPrediction)
    {
        var currentTestStep = "Create donors then run original search";
        steps.Logger.LogStart(currentTestStep);
        var firstDonors = await CreateFirstDonors(steps, donorType);
        var searchId = await steps.OriginalSearchShouldOnlyReturnExpectedDonors(requestFileName, firstDonors, parallelMatchPrediction);
        steps.Logger.LogCompletion(currentTestStep);

        currentTestStep = "Apply donor updates then run repeat search";
        steps.Logger.LogStart(currentTestStep);
        var timeBeforeDonorChanges = DateTimeOffset.UtcNow;
        var donorChanges = await ApplyDonorChanges(steps, donorType, firstDonors);
        await RepeatSearchShouldIdentifyExpectedChanges(steps, requestFileName, searchId, timeBeforeDonorChanges, donorChanges, parallelMatchPrediction);
        steps.Logger.LogCompletion(currentTestStep);

        currentTestStep = "Delete previously matched donors then run repeat search";
        steps.Logger.LogStart(currentTestStep);
        timeBeforeDonorChanges = DateTimeOffset.UtcNow;
        donorChanges = await DeleteDonors(steps, donorChanges.NewlyMatching);
        await RepeatSearchShouldIdentifyExpectedChanges(steps, requestFileName, searchId, timeBeforeDonorChanges, donorChanges, parallelMatchPrediction);
        steps.Logger.LogCompletion(currentTestStep);
    }

    private static async Task<DonorChanges> CreateFirstDonors(
        RepeatSearchTestSteps steps,
        ImportDonorType donorType)
    {
        var nonMatchingDonor = await steps.CreateDonor(donorType, ImportedHlaBuilder.ValidDnaPhenotype);
        var matchingDonor = await steps.CreateDonor(donorType, ImportedHlaBuilder.SearchTestPhenotype);
        return new DonorChanges
        {
            NoLongerMatching = new[] { nonMatchingDonor },
            NewlyMatching = new[] { matchingDonor }
        };
    }

    private static async Task<DonorChanges> ApplyDonorChanges(
        RepeatSearchTestSteps steps,
        ImportDonorType donorType,
        DonorChanges originalDonorChanges)
    {
        foreach (var donorCode in originalDonorChanges.NewlyMatching)
        {
            await steps.EditDonorHla(donorCode, donorType, ImportedHlaBuilder.ValidDnaPhenotype);
        }

        foreach (var donorCode in originalDonorChanges.NoLongerMatching)
        {
            await steps.EditDonorHla(donorCode, donorType, ImportedHlaBuilder.SearchTestPhenotype);
        }

        var newMatchingDonor = await steps.CreateDonor(donorType, ImportedHlaBuilder.SearchTestPhenotype);

        return new DonorChanges
        {
            NoLongerMatching = originalDonorChanges.NewlyMatching,
            NewlyMatching = originalDonorChanges.NoLongerMatching.Append(newMatchingDonor).ToList()
        };
    }

    private static async Task<DonorChanges> DeleteDonors(
        RepeatSearchTestSteps steps,
        IReadOnlyCollection<string> donors)
    {
        await steps.DeleteDonors(donors);

        return new DonorChanges
        {
            NoLongerMatching = donors,
            NewlyMatching = new List<string>()
        };
    }

    private static async Task RepeatSearchShouldIdentifyExpectedChanges(
        RepeatSearchTestSteps steps,
        string searchRequestFileName,
        string originalSearchId,
        DateTimeOffset searchCutOff,
        DonorChanges donorChanges,
        bool? parallelMatchPrediction)
    {
        var repeatRunId = await steps.SubmitRepeatSearchRequest(searchRequestFileName, originalSearchId, searchCutOff, parallelMatchPrediction);
        await steps.RepeatMatchingShouldHaveIdentifiedExpectedChanges(repeatRunId, originalSearchId, donorChanges);
        await steps.RepeatSearchShouldHaveIdentifiedExpectedChanges(repeatRunId, originalSearchId, donorChanges);
    }
}
