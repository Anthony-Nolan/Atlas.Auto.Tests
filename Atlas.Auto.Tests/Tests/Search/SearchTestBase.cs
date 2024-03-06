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
        var testLogger = new TestLogger(ExtentManager.CreateForTest(TestFixtureName, testName));
        var importStepsForSearchTests = ResolveDonorImportStepsForSearchTests(testLogger);
        return ResolveTestServices(importStepsForSearchTests, testLogger, testName);
    }

    private DonorImportStepsForSearchTests ResolveDonorImportStepsForSearchTests(ITestLogger testLogger)
    {
        var donorImportWorkflow = Provider.ResolveServiceOrThrow<IDonorImportWorkflow>();
        var donorImportTestSteps = new DonorImportTestSteps(donorImportWorkflow, testLogger);
        return new DonorImportStepsForSearchTests(donorImportTestSteps, testLogger);
    }

    private TestServices<ISearchTestSteps> ResolveTestServices(
        IDonorImportStepsForSearchTests importStepsForSearchTests,
        ITestLogger testLogger, 
        string testName)
    {
        var searchWorkflow = Provider.ResolveServiceOrThrow<ISearchWorkflow>();
        var searchTestSteps = new SearchTestSteps(searchWorkflow, importStepsForSearchTests, testLogger, testName);
        return new TestServices<ISearchTestSteps>(searchTestSteps, testLogger);
    }
}