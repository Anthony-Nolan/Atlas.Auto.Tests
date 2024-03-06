using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;

namespace Atlas.Auto.Tests.Tests.DonorImport;

internal abstract class DonorImportTestBase : TestBase
{
    protected DonorImportTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected TestServices<IDonorImportTestSteps> GetTestServices(string testName)
    {
        var workflow = Provider.ResolveServiceOrThrow<IDonorImportWorkflow>();
        var testLogger = BuildTestLogger(testName);
        return new TestServices<IDonorImportTestSteps>(new DonorImportTestSteps(workflow, testLogger), testLogger);
    }
}