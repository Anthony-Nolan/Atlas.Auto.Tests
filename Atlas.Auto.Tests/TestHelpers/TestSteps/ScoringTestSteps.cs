using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Client.Models.Scoring.Requests;
using Atlas.Debug.Client.Clients;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class ScoringTestSteps
{
    private readonly IPublicApiFunctionsClient _publicApiClient;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;
    private readonly string _testName;
    public ITestLogger Logger { get; }

    public ScoringTestSteps(IServiceProvider provider, ITestLogger logger, string testName)
    {
        _publicApiClient = provider.GetRequiredService<IPublicApiFunctionsClient>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
        Logger = logger;
        _testName = testName;
    }

    public async Task DonorBatchShouldBeScored(string scoringRequestFileName)
    {
        var scoreRequest = await SourceDataReader.ReadJsonFile<DonorHlaBatchScoringRequest>(scoringRequestFileName);

        var result = await _pollyRetry.ExecuteWithRetry(
            async () => await _publicApiClient.PostScoreBatch(scoreRequest),
            _retry.ApiCall, $"Score batch request '{scoringRequestFileName}'");
        result.Should().NotBeNull("batch scoring should have returned results");

        var scoringResult = result!.SerializeCollection();
        await VerifyJson(scoringResult)
            .WriteReceivedToApprovalsFolder(_testName)
            .IgnoreVaryingSearchResultProperties();
    }

    public async Task DonorShouldBeScored(string scoringRequestFileName)
    {
        var scoreRequest = await SourceDataReader.ReadJsonFile<DonorHlaScoringRequest>(scoringRequestFileName);

        var result = await _pollyRetry.ExecuteWithRetry(
            async () => await _publicApiClient.PostScore(scoreRequest),
            _retry.ApiCall, $"Score request '{scoringRequestFileName}'");
        result.Should().NotBeNull("scoring should have returned a result");

        var scoringResult = result!.SerializeSingle();
        await VerifyJson(scoringResult)
            .WriteReceivedToApprovalsFolder(_testName)
            .IgnoreVaryingSearchResultProperties();
    }
}
