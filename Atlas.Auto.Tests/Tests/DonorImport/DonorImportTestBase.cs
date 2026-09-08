using Atlas.Auto.Tests.TestHelpers.TestSteps;

namespace Atlas.Auto.Tests.Tests.DonorImport;

internal abstract class DonorImportTestBase : TestBase
{
    protected DonorImportTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected DonorImportTestSteps GetDonorImportTestSteps(string testName)
    {
        return new DonorImportTestSteps(Provider, BuildTestLogger(testName));
    }
}
