using Microsoft.Extensions.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;

namespace Atlas.Auto.Tests.Tests.DonorImport;

internal abstract class DonorImportTestBase : TestBase
{
    protected DonorImportTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected DonorImportTestSteps GetDonorImportTestSteps(string testName)
    {
        var workflow = Provider.GetRequiredService<DonorImportWorkflow>();
        var testLogger = BuildTestLogger(testName);
        return new DonorImportTestSteps(workflow, testLogger);
    }
}
