using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;

namespace Atlas.Auto.Tests.Tests.Search;

internal abstract class SearchTestBase : TestBase
{
    protected SearchTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected TestServices<ISearchTestSteps> GetSearchTestServices(string testName)
    {
        var testLogger = BuildTestLogger(testName);
        var importStepsForSearchTests = ResolveDonorImportStepsForSearchTests(testLogger);
        return ResolveSearchTestServices(importStepsForSearchTests, testLogger, testName);
    }

    protected DonorImportStepsForSearchTests ResolveDonorImportStepsForSearchTests(ITestLogger testLogger)
    {
        var donorImportWorkflow = Provider.ResolveServiceOrThrow<IDonorImportWorkflow>();
        var donorImportTestSteps = new DonorImportTestSteps(donorImportWorkflow, testLogger);
        return new DonorImportStepsForSearchTests(donorImportTestSteps, testLogger);
    }

    private TestServices<ISearchTestSteps> ResolveSearchTestServices(
        IDonorImportStepsForSearchTests importStepsForSearchTests,
        ITestLogger testLogger, 
        string testName)
    {
        var searchWorkflow = Provider.ResolveServiceOrThrow<ISearchWorkflow>();
        var searchTestSteps = new SearchTestSteps(searchWorkflow, importStepsForSearchTests, testLogger, testName);
        return new TestServices<ISearchTestSteps>(searchTestSteps, testLogger);
    }
}