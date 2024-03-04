using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Auto.Utils.Reporting;

namespace Atlas.Auto.Tests.Tests.Scoring;

internal abstract class ScoringTestBase : TestBase
{
    protected ScoringTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected TestServices<IScoringTestSteps> GetTestServices(string testName)
    {
        var scoringWorkflow = Provider.ResolveServiceOrThrow<IScoringWorkflow>();
        var testLogger = new TestLogger(ExtentManager.CreateForTest(TestFixtureName, testName));
        return new TestServices<IScoringTestSteps>(new ScoringTestSteps(scoringWorkflow, testLogger, testName), testLogger);
    }
}