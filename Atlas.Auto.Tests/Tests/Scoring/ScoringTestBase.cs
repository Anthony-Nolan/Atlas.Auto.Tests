using Microsoft.Extensions.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.Tests.Scoring;

internal abstract class ScoringTestBase : TestBase
{
    protected ScoringTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected ScoringTestSteps GetScoringTestSteps(string testName)
    {
        return new ScoringTestSteps(
            Provider.GetRequiredService<IPublicApiFunctionsClient>(),
            Provider.GetRequiredService<PollyRetry>(),
            Provider.GetRequiredService<RetrySettings>(),
            BuildTestLogger(testName),
            testName);
    }
}
