using Atlas.Auto.Tests.TestHelpers.Assertions;
using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Attributes;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using System.Runtime.CompilerServices;

namespace Atlas.Auto.Tests.Tests.Search;

[TestFixture]
[Category(nameof(SearchExceptionPathTests))]
internal class SearchExceptionPathTests : SearchTestBase
{
    public SearchExceptionPathTests() : base(nameof(SearchExceptionPathTests))
    {
    }

    [Test]
    public async Task Search_MissingRequiredInfo_ReturnsValidationError()
    {
        await RunTest(async steps =>
        {
            var validationFailures = await steps.SubmitInvalidSearchRequest("search-request-missing-locus-A.json");
            validationFailures.ToList().ShouldContain(@"'A' must not be empty.");
        });
    }

    /// <summary>
    /// QA-2920: a search with patient HLA Atlas does not know fails in matching, and search tracking records the failure:
    /// one failed attempt, no match prediction, every event sent and none dead-lettered.
    /// </summary>
    [Test]
    [ManualTest("QA-1980")]
    [AutoTest("QA-2920")]
    public async Task Search_InvalidPatientHla_FailsDuringMatching()
    {
        // HLA at A position 1 of the request file
        const string invalidHla = "invalid-hla";
        var expectedFailure = ExpectedMatchingFailure.HlaLookup(invalidHla, "A");

        await RunTest(async steps =>
        {
            // Matching fails and says so in its notification
            var searchResponse = await steps.SubmitSearchRequest("search-request-invalid-patient-hla-at-A1.json");
            await steps.MatchingShouldHaveFailedValidation(searchResponse.SearchIdentifier, expectedFailure.Message);

            // Search tracking stores the failure and the failed attempt
            var tracking = await steps.SearchTracking.MatchingFailureShouldHaveBeenTracked(
                searchResponse.SearchIdentifier, expectedFailure, originalSearchId: null);

            // Every event went through search-tracking-events and was processed
            await steps.SearchTracking.EventsShouldHaveBeenSent(tracking, originalSearchId: null,
            [
                SearchTrackingEventTypes.SearchRequested,
                SearchTrackingEventTypes.MatchingAlgorithmAttemptStarted,
                SearchTrackingEventTypes.MatchingAlgorithmCompleted,
                SearchTrackingEventTypes.MatchPredictionResultsSent
            ]);
            await steps.SearchTracking.EventsShouldNotHaveBeenDeadLettered(tracking);

            // No MatchPredictionStarted among the events, so no match prediction can be stored
            await steps.SearchTracking.MatchPredictionShouldNotHaveBeenStored(tracking);
        });
    }

    private async Task RunTest(
        Func<SearchTestSteps, Task> action,
        [CallerMemberName] string callerName = "")
    {
        await ExecuteWithRetry(async () =>
        {
            var steps = GetSearchTestSteps(callerName);
            await action(steps);
        });
    }
}
