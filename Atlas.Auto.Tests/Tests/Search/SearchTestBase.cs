using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Auto.Utils.Reporting;

namespace Atlas.Auto.Tests.Tests.Search;

internal abstract class SearchTestBase : TestBase
{
    protected SearchTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected TestServices<ISearchTestSteps> GetTestServices(string testName)
    {
        var searchWorkflow = Provider.ResolveServiceOrThrow<ISearchWorkflow>();
        var donorImportWorkflow = Provider.ResolveServiceOrThrow<IDonorImportWorkflow>();
        var testLogger = new TestLogger(ExtentManager.CreateForTest(TestFixtureName, testName));
        var donorImportTestSteps = new DonorImportTestSteps(donorImportWorkflow, testLogger);
        return new TestServices<ISearchTestSteps>(new SearchTestSteps(searchWorkflow, donorImportTestSteps, testLogger, testName), testLogger);
    }
}