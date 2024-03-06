using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Auto.Tests.Tests.Search;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

internal abstract class RepeatSearchTestBase : SearchTestBase
{
    protected RepeatSearchTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected TestServices<IRepeatSearchTestSteps> GetRepeatSearchTestServices(string testName)
    {
        var searchTestServices = GetSearchTestServices(testName); // builds search test steps & a test logger
        var repeatSearchTestSteps = ResolveRepeatSearchTestSteps(searchTestServices, testName);
        return new TestServices<IRepeatSearchTestSteps>(repeatSearchTestSteps, searchTestServices.Logger);
    }

    private RepeatSearchTestSteps ResolveRepeatSearchTestSteps(
        TestServices<ISearchTestSteps> searchTestServices,
        string testName)
    {
        var repeatWorkflow = Provider.ResolveServiceOrThrow<IRepeatSearchWorkflow>();
        var importStepsForSearchTests = ResolveDonorImportStepsForSearchTests(searchTestServices.Logger);
        return new RepeatSearchTestSteps(
            repeatWorkflow, importStepsForSearchTests, searchTestServices.Steps, searchTestServices.Logger, testName);
    }
}