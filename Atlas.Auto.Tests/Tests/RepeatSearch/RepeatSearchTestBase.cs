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
        return new RepeatSearchTestSteps(Provider, searchTestSteps, searchTestSteps.DonorImportSteps, searchTestSteps.Logger, testName);
    }
}
