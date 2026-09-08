using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.Tests;

namespace Atlas.Auto.Tests;

[SetUpFixture]
internal class TestsSetUp
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpForAllTests()
    {
        var testDonorDeleter = new TestDonorDeleter(TestBase.Provider);
        await testDonorDeleter.DeleteDonors();
    }
}
