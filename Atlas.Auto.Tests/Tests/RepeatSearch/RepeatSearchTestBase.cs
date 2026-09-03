using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.Tests.Search;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

internal abstract class RepeatSearchTestBase : SearchTestBase
{
    protected RepeatSearchTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected RepeatSearchTestSteps GetRepeatSearchTestSteps(string testName)
    {
        var searchTestSteps = GetSearchTestSteps(testName);
        var importSteps = ResolveDonorImportStepsForSearchTests(searchTestSteps.Logger);
        return new RepeatSearchTestSteps(Provider, searchTestSteps, importSteps, searchTestSteps.Logger, testName);
    }
}
