using Atlas.Auto.Tests.TestHelpers.TestSteps;

namespace Atlas.Auto.Tests.Tests.Scoring;

internal abstract class ScoringTestBase : TestBase
{
    protected ScoringTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected ScoringTestSteps GetScoringTestSteps(string testName)
    {
        return new ScoringTestSteps(Provider, BuildTestLogger(testName), testName);
    }
}
