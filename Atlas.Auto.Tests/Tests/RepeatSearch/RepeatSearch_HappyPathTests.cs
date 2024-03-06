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
        const string searchRequestFileName = "search-request-donor-10_10.json";
        var test = GetRepeatSearchTestServices(testName);

        const string testDescription = "Repeat Search of 10/10 donor search";
        test.Logger.LogStart(testDescription);

        var currentTestStep = "Create matching donor and run original search";
        test.Logger.LogStart(currentTestStep);
        var firstDonorId = await test.Steps.CreateDonor(donorType);
        var originalSearchId = await test.Steps.OriginalSearchShouldReturnExpectedDonor(searchRequestFileName, firstDonorId);
        test.Logger.LogCompletion(currentTestStep);

        // time after original search completion and before donor changes are made
        var searchCutOff = DateTimeOffset.UtcNow;

        currentTestStep = "Edit first donor to no longer match & create second matching donor";
        test.Logger.LogStart(currentTestStep);
        await test.Steps.EditDonorToNoLongerMatch(firstDonorId, donorType);
        var secondDonorId = await test.Steps.CreateDonor(donorType);
        var firstRepeatRunId = await test.Steps.SubmitRepeatSearchRequest(searchRequestFileName, originalSearchId, searchCutOff);
        await test.Steps.RepeatMatchingShouldHaveIdentifiedExpectedChanges(firstRepeatRunId, originalSearchId, firstDonorId, secondDonorId);
        await test.Steps.RepeatSearchShouldHaveIdentifiedExpectedChanges(firstRepeatRunId, originalSearchId, firstDonorId, secondDonorId);
        test.Logger.LogCompletion(currentTestStep);
        
        // Reset search cut off time to time after first repeat search completion and before 2nd set of donor changes are made
        searchCutOff = DateTimeOffset.UtcNow;

        currentTestStep = "Delete second donor";
        test.Logger.LogStart(currentTestStep);
        await test.Steps.DeleteDonor(secondDonorId);
        var secondRepeatRunId = await test.Steps.SubmitRepeatSearchRequest(searchRequestFileName, originalSearchId, searchCutOff);
        await test.Steps.RepeatMatchingShouldHaveIdentifiedExpectedChanges(secondRepeatRunId, originalSearchId, secondDonorId, null);
        await test.Steps.RepeatSearchShouldHaveIdentifiedExpectedChanges(secondRepeatRunId, originalSearchId, secondDonorId, null);
        test.Logger.LogCompletion(currentTestStep);

        test.Logger.LogCompletion(testDescription);
    }
}