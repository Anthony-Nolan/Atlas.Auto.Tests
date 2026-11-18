using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Attributes;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Settings;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

[TestFixture]
[Category(nameof(RepeatSearchExceptionPathTests))]
internal class RepeatSearchExceptionPathTests : RepeatSearchTestBase
{
    public RepeatSearchExceptionPathTests() : base(nameof(RepeatSearchExceptionPathTests))
    {
    }

    [Test]
    public async Task RepeatSearch_MissingRequiredInfo_ReturnsValidationError()
    {
        var steps = GetRepeatSearchTestSteps(nameof(RepeatSearch_MissingRequiredInfo_ReturnsValidationError));
        await steps.RepeatRequestMissingRequiredInfoShouldReturnValidationErrors();
    }

    /// <summary>
    /// QA-2921: the public API accepts a repeat search of a search Atlas has no record of. ATL-527: the repeat search waits
    /// for the original results until the maximum wait, then matching fails validation, and search tracking records the failure:
    /// one failed attempt, no match prediction, every event sent and none dead-lettered.
    /// </summary>
    /// <remarks>
    /// Changes app settings of the repeat search function for the run (RepeatSearchSetup, <see cref="FunctionAppTestSettings"/>):
    /// RepeatSearch:OriginalResultsWaitIntervalSeconds = 20 (deployed on DEV: 300) and
    /// RepeatSearch:MaxWaitForOriginalResultsMinutes = 1 (deployed on DEV: 1440, so the test would wait 24 h).
    /// Skipped when Azure:ChangeFunctionAppSettings is false.
    /// </remarks>
    [Test]
    [RequiresAppSetting(FunctionAppTestSettings.OriginalResultsWaitInterval)]
    [RequiresAppSetting(FunctionAppTestSettings.MaxWaitForOriginalResults)]
    [ManualTest("QA-1980")]
    [AutoTest("QA-2921")]
    public async Task RepeatSearch_UnknownOriginalSearch_FailsValidationAfterWait()
    {
        var steps = GetRepeatSearchTestSteps(nameof(RepeatSearch_UnknownOriginalSearch_FailsValidationAfterWait));
        // A new ID, so no search has it
        var originalSearchId = Guid.NewGuid().ToString();
        var expectedFailure = ExpectedMatchingFailure.NoOriginalSearch(originalSearchId);

        // After the wait, matching fails validation and says so in its notification
        var repeatSearchId = await steps.SubmitRepeatSearchRequest(
            "search-request-donor-10_10.json", originalSearchId, DateTimeOffset.UtcNow);
        await steps.RepeatMatchingShouldHaveFailedValidation(repeatSearchId, originalSearchId, expectedFailure.Message);

        // Search tracking stores the failure under the repeat search ID and the failed attempt
        var tracking = await steps.SearchTracking.MatchingFailureShouldHaveBeenTracked(
            repeatSearchId, expectedFailure, originalSearchId);

        // The attempt started only after the maximum wait from the request
        await steps.RepeatMatchingShouldHaveWaitedForOriginalResults(tracking);

        // Every event went through search-tracking-events and was processed; the wait sends none
        var sentEvents = await steps.SearchTracking.EventsShouldHaveBeenSent(tracking, originalSearchId,
        [
            SearchTrackingEventTypes.SearchRequested,
            SearchTrackingEventTypes.MatchingAlgorithmAttemptStarted,
            SearchTrackingEventTypes.MatchingAlgorithmCompleted,
            SearchTrackingEventTypes.MatchPredictionResultsSent
        ]);
        await steps.SearchTracking.EventsShouldNotHaveBeenDeadLettered(tracking);

        // Match prediction never started, so none is stored
        await steps.SearchTracking.MatchPredictionShouldNotHaveBeenStored(tracking, sentEvents);
    }
}
