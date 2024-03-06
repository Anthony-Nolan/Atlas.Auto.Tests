using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;

namespace Atlas.Auto.Tests.Tests.Scoring;

internal abstract class ScoringTestBase : TestBase
{
    protected ScoringTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected TestServices<IScoringTestSteps> GetTestServices(string testName)
    {
        var scoringWorkflow = Provider.ResolveServiceOrThrow<IScoringWorkflow>();
        var testLogger = BuildTestLogger(testName);
        return new TestServices<IScoringTestSteps>(new ScoringTestSteps(scoringWorkflow, testLogger, testName), testLogger);
    }
}