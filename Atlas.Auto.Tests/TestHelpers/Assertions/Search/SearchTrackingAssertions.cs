using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.Search;

internal static class SearchTrackingAssertions
{
    /// <summary>
    /// The request time is set by the Atlas function, the submit time by the test machine: allow for the time the
    /// request takes and for clock differences.
    /// </summary>
    private static readonly TimeSpan RequestTimeTolerance = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The request row holds the search the test sent.
    /// </summary>
    /// <returns>
    /// The request as matching got it, with ParallelMatchPrediction and UsePrecomputedGenotypeSets always set:
    /// the public API fills them in when the test leaves them empty.
    /// </returns>
    public static SearchRequest ShouldHoldSubmittedSearch(this SearchTrackingRequest request, SubmittedSearch submitted)
    {
        var sent = submitted.Request;
        var stored = JsonConvert.DeserializeObject<SearchRequest>(request.RequestJson)!;
        var storedRegistryCodes = JsonConvert.DeserializeObject<List<string>>(request.DonorRegistryCodes ?? "null");

        using (new AssertionScope())
        {
            // New search, not a repeat search
            request.IsRepeatSearch.Should().BeFalse();
            request.OriginalSearchIdentifier.Should().BeNull();
            request.RepeatSearchCutOffDate.Should().BeNull();

            // Criteria as sent
            request.SearchCriteria.Should().Be(ExpectedSearchCriteria(sent));
            request.DonorType.Should().Be(sent.SearchDonorType.ToString());
            storedRegistryCodes.Should().Equal(sent.DonorRegistryCodes);
            request.AreBetterMatchesIncluded.Should().Be(sent.MatchCriteria.IncludeBetterMatches);
            request.IsMatchPredictionRun.Should().Be(sent.RunMatchPrediction);
            request.RequestTimeUtc.Should().BeCloseTo(submitted.SubmittedAtUtc, RequestTimeTolerance);

            // Request JSON as sent, except the flags the public API fills in when the test leaves them empty
            stored.Should().BeEquivalentTo(sent, options => options
                .Excluding(r => r.ParallelMatchPrediction)
                .Excluding(r => r.UsePrecomputedGenotypeSets));
            stored.ParallelMatchPrediction.Should().NotBeNull("the public API sets it when the request leaves it empty");
            stored.UsePrecomputedGenotypeSets.Should().NotBeNull("the public API sets it when the request leaves it empty");
            if (sent.ParallelMatchPrediction != null)
                stored.ParallelMatchPrediction.Should().Be(sent.ParallelMatchPrediction, "the public API keeps a value the request sets");
            if (sent.UsePrecomputedGenotypeSets != null)
                stored.UsePrecomputedGenotypeSets.Should().Be(sent.UsePrecomputedGenotypeSets, "the public API keeps a value the request sets");
        }

        return stored;
    }

    /// <summary>
    /// Matching found results on its first attempt and sent them: the same count and HLA version as its notification.
    /// </summary>
    public static void MatchingShouldHaveSucceeded(
        this SearchTrackingRequest request, MatchingResultsNotification notification)
    {
        // Precondition: matching scores only the donors it found, so without results there are no scoring times
        notification.NumberOfResults.Should().BePositive(
            "the test should create a donor the search finds, for search {0}", request.SearchIdentifier);

        using (new AssertionScope())
        {
            request.MatchingAlgorithm_IsSuccessful.Should().BeTrue();
            request.MatchingAlgorithm_FailureInfo_Type.Should().BeNull();
            request.MatchingAlgorithm_FailureInfo_Message.Should().BeNull();
            request.MatchingAlgorithm_FailureInfo_ExceptionStacktrace.Should().BeNull();
            request.MatchingAlgorithm_TotalAttemptsNumber.Should().Be(1);
            request.MatchingAlgorithm_NumberOfResults.Should().Be(notification.NumberOfResults);
            request.MatchingAlgorithm_HlaNomenclatureVersion.Should().Be(notification.MatchingAlgorithmHlaNomenclatureVersion);
            request.MatchingAlgorithm_ResultsSent.Should().BeTrue();
            request.MatchingAlgorithm_ResultsSentTimeUtc.Should().NotBeNull();

            // Only a repeat search counts changed results
            request.MatchingAlgorithm_RepeatSearch_AddedResultCount.Should().BeNull();
            request.MatchingAlgorithm_RepeatSearch_RemovedResultCount.Should().BeNull();
            request.MatchingAlgorithm_RepeatSearch_UpdatedResultCount.Should().BeNull();
        }
    }

    /// <summary>
    /// Match prediction ran to the end and the final results were sent after it saved them.
    /// </summary>
    public static void MatchPredictionShouldHaveSucceeded(
        this SearchTrackingRequest request, SearchTrackingMatchPrediction matchPrediction)
    {
        using (new AssertionScope())
        {
            request.MatchPrediction_IsSuccessful.Should().BeTrue();
            request.MatchPrediction_FailureInfo_Type.Should().BeNull();
            request.MatchPrediction_FailureInfo_Message.Should().BeNull();
            request.MatchPrediction_FailureInfo_ExceptionStacktrace.Should().BeNull();
            request.MatchPrediction_DonorsPerBatch.Should().BePositive();
            request.MatchPrediction_TotalNumberOfBatches.Should().BePositive();
            request.ResultsSent.Should().BeTrue();
            request.ResultsSentTimeUtc.Should().NotBeNull();
            // Without the persisting time the match prediction check fails on its own
            if (matchPrediction.PersistingResults_EndTimeUtc is { } persistedAt)
                request.ResultsSentTimeUtc.Should().BeOnOrAfter(persistedAt, "results are sent once match prediction has saved them");
        }
    }

    /// <summary>
    /// The search asked for no match prediction: none ran and no final results were sent.
    /// </summary>
    public static void MatchPredictionShouldNotHaveRun(this SearchTrackingRequest request)
    {
        using (new AssertionScope())
        {
            request.MatchPrediction_IsSuccessful.Should().BeNull();
            request.MatchPrediction_FailureInfo_Type.Should().BeNull();
            request.MatchPrediction_FailureInfo_Message.Should().BeNull();
            request.MatchPrediction_FailureInfo_ExceptionStacktrace.Should().BeNull();
            request.MatchPrediction_DonorsPerBatch.Should().BeNull();
            request.MatchPrediction_TotalNumberOfBatches.Should().BeNull();
            request.ResultsSent.Should().BeNull();
            request.ResultsSentTimeUtc.Should().BeNull();
        }
    }

    /// <summary>
    /// The first attempt found results: every stage has a time, the stages follow each other, no failure.
    /// </summary>
    /// <remarks>
    /// Scoring reads the matches as matching streams them, so scoring starts before matching ends: the two run side
    /// by side, and each starts after matching started and ends after matching ended.
    /// </remarks>
    public static void ShouldHaveSucceeded(this SearchTrackingMatchingAlgorithmAttempt attempt)
    {
        using (new AssertionScope())
        {
            attempt.AttemptNumber.Should().Be(1);

            // Matching, then saving the results
            TimesShouldBeInOrder(
                (nameof(attempt.InitiationTimeUtc), attempt.InitiationTimeUtc),
                (nameof(attempt.StartTimeUtc), attempt.StartTimeUtc),
                (nameof(attempt.AlgorithmCore_Matching_StartTimeUtc), attempt.AlgorithmCore_Matching_StartTimeUtc),
                (nameof(attempt.AlgorithmCore_Matching_EndTimeUtc), attempt.AlgorithmCore_Matching_EndTimeUtc),
                (nameof(attempt.AlgorithmCore_Scoring_EndTimeUtc), attempt.AlgorithmCore_Scoring_EndTimeUtc),
                (nameof(attempt.PersistingResults_StartTimeUtc), attempt.PersistingResults_StartTimeUtc),
                (nameof(attempt.PersistingResults_EndTimeUtc), attempt.PersistingResults_EndTimeUtc),
                (nameof(attempt.CompletionTimeUtc), attempt.CompletionTimeUtc));

            // Scoring, side by side with matching
            TimesShouldBeInOrder(
                (nameof(attempt.AlgorithmCore_Matching_StartTimeUtc), attempt.AlgorithmCore_Matching_StartTimeUtc),
                (nameof(attempt.AlgorithmCore_Scoring_StartTimeUtc), attempt.AlgorithmCore_Scoring_StartTimeUtc),
                (nameof(attempt.AlgorithmCore_Scoring_EndTimeUtc), attempt.AlgorithmCore_Scoring_EndTimeUtc));

            attempt.IsSuccessful.Should().BeTrue();
            attempt.FailureInfo_Type.Should().BeNull();
            attempt.FailureInfo_Message.Should().BeNull();
            attempt.FailureInfo_ExceptionStacktrace.Should().BeNull();
        }
    }

    /// <summary>
    /// Match prediction ran the way the search asked for: every stage has a time, the stages follow each other,
    /// no failure.
    /// </summary>
    /// <param name="searchedRequest">The request as matching got it: see <see cref="ShouldHoldSubmittedSearch"/>.</param>
    public static void ShouldHaveSucceeded(this SearchTrackingMatchPrediction matchPrediction, SearchRequest searchedRequest)
    {
        using (new AssertionScope())
        {
            matchPrediction.IsParallelMatchPrediction.Should().Be(searchedRequest.ParallelMatchPrediction!.Value);
            matchPrediction.IsPrecomputedMatchPrediction.Should().Be(searchedRequest.UsePrecomputedGenotypeSets!.Value);

            // Parallel batches all write the running times, so they hold the last batch's: still in this order
            TimesShouldBeInOrder(
                (nameof(matchPrediction.InitiationTimeUtc), matchPrediction.InitiationTimeUtc),
                (nameof(matchPrediction.StartTimeUtc), matchPrediction.StartTimeUtc),
                (nameof(matchPrediction.PrepareBatches_StartTimeUtc), matchPrediction.PrepareBatches_StartTimeUtc),
                (nameof(matchPrediction.PrepareBatches_EndTimeUtc), matchPrediction.PrepareBatches_EndTimeUtc),
                (nameof(matchPrediction.AlgorithmCore_RunningBatches_StartTimeUtc), matchPrediction.AlgorithmCore_RunningBatches_StartTimeUtc),
                (nameof(matchPrediction.AlgorithmCore_RunningBatches_EndTimeUtc), matchPrediction.AlgorithmCore_RunningBatches_EndTimeUtc),
                (nameof(matchPrediction.PersistingResults_StartTimeUtc), matchPrediction.PersistingResults_StartTimeUtc),
                (nameof(matchPrediction.PersistingResults_EndTimeUtc), matchPrediction.PersistingResults_EndTimeUtc),
                (nameof(matchPrediction.CompletionTimeUtc), matchPrediction.CompletionTimeUtc));

            matchPrediction.IsSuccessful.Should().BeTrue();
            matchPrediction.FailureInfo_Type.Should().BeNull();
            matchPrediction.FailureInfo_Message.Should().BeNull();
            matchPrediction.FailureInfo_ExceptionStacktrace.Should().BeNull();
        }
    }

    /// <summary>
    /// Atlas SearchTrackingEventHelper.GetSearchCriteria: positions searched minus mismatches allowed, out of
    /// positions searched (two per locus with mismatch criteria), e.g. "10/10" or "9/10".
    /// </summary>
    private static string ExpectedSearchCriteria(SearchRequest request)
    {
        var criteria = request.MatchCriteria.LocusMismatchCriteria;
        var lociSearched = new[] { criteria.A, criteria.B, criteria.C, criteria.Dpb1, criteria.Dqb1, criteria.Drb1 }
            .Count(mismatches => mismatches.HasValue);
        var positionsSearched = lociSearched * 2;
        return $"{positionsSearched - request.MatchCriteria.DonorMismatchCount}/{positionsSearched}";
    }

    /// <summary>
    /// Every time is set and none is earlier than the one before it.
    /// </summary>
    private static void TimesShouldBeInOrder(params (string Name, DateTime? Time)[] times)
    {
        foreach (var (name, time) in times)
            time.Should().NotBeNull("{0} should be set", name);

        for (var i = 1; i < times.Length; i++)
        {
            var (previousName, previousTime) = times[i - 1];
            var (name, time) = times[i];
            if (previousTime != null && time != null)
                time.Should().BeOnOrAfter(previousTime.Value, "{0} should not be earlier than {1}", name, previousName);
        }
    }

    /// <param name="originalSearchId">Null for a new search; the original search ID for a repeat search.</param>
    public static void MatchingShouldHaveFailed(
        this SearchTrackingRequest request, ExpectedMatchingFailure expected, string? originalSearchId)
    {
        using (new AssertionScope())
        {
            // Search or repeat search
            request.IsRepeatSearch.Should().Be(originalSearchId != null);
            request.OriginalSearchIdentifier.Should().Be(originalSearchId?.ToLowerInvariant());

            // Matching failed on its only attempt and found nothing
            request.MatchingAlgorithm_IsSuccessful.Should().BeFalse();
            request.MatchingAlgorithm_FailureInfo_Type.Should().Be(expected.Type);
            request.MatchingAlgorithm_FailureInfo_Message.Should().Be(expected.Message);
            // Not the exact text: it holds build paths and line numbers
            request.MatchingAlgorithm_FailureInfo_ExceptionStacktrace.Should().NotBeNullOrWhiteSpace();
            request.MatchingAlgorithm_TotalAttemptsNumber.Should().Be(1);
            request.MatchingAlgorithm_NumberOfResults.Should().BeNull();
            request.MatchingAlgorithm_HlaNomenclatureVersion.Should().NotBeNullOrWhiteSpace();
            request.MatchingAlgorithm_ResultsSent.Should().BeTrue();
            request.MatchingAlgorithm_ResultsSentTimeUtc.Should().NotBeNull();

            // Match prediction never ran, but the failure notification was sent
            request.MatchPrediction_IsSuccessful.Should().BeNull();
            request.MatchPrediction_FailureInfo_Type.Should().BeNull();
            request.MatchPrediction_FailureInfo_Message.Should().BeNull();
            request.MatchPrediction_FailureInfo_ExceptionStacktrace.Should().BeNull();
            request.MatchPrediction_DonorsPerBatch.Should().BeNull();
            request.MatchPrediction_TotalNumberOfBatches.Should().BeNull();
            request.ResultsSent.Should().BeTrue();
            request.ResultsSentTimeUtc.Should().NotBeNull();
        }
    }

    /// <summary>
    /// The attempt failed before matching started: no matching, scoring or persisting timings.
    /// </summary>
    public static void ShouldHaveFailedBeforeMatching(
        this SearchTrackingMatchingAlgorithmAttempt attempt, ExpectedMatchingFailure expected)
    {
        using (new AssertionScope())
        {
            attempt.AttemptNumber.Should().Be(1);
            attempt.InitiationTimeUtc.Should().NotBe(default);
            attempt.StartTimeUtc.Should().NotBe(default);
            attempt.CompletionTimeUtc.Should().NotBeNull();

            attempt.AlgorithmCore_Matching_StartTimeUtc.Should().BeNull();
            attempt.AlgorithmCore_Matching_EndTimeUtc.Should().BeNull();
            attempt.AlgorithmCore_Scoring_StartTimeUtc.Should().BeNull();
            attempt.AlgorithmCore_Scoring_EndTimeUtc.Should().BeNull();
            attempt.PersistingResults_StartTimeUtc.Should().BeNull();
            attempt.PersistingResults_EndTimeUtc.Should().BeNull();

            attempt.IsSuccessful.Should().BeFalse();
            attempt.FailureInfo_Type.Should().Be(expected.Type);
            attempt.FailureInfo_Message.Should().Be(expected.Message);
            attempt.FailureInfo_ExceptionStacktrace.Should().NotBeNullOrWhiteSpace();
        }
    }
}
