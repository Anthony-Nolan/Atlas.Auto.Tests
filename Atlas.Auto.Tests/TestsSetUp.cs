using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services.DonorDeletion;

namespace Atlas.Auto.Tests
{
    [SetUpFixture]
    public class TestsSetUp
    {
        [OneTimeSetUp]
        public async Task OneTimeSetUpForAllTests()
        {
            DependencyInjection.DependencyInjection.Provider = ServiceConfiguration.CreateProvider();

            // Intentionally deleting donors during SetUp instead of Teardown
            // to allow inspection of debug data after test run completion
            var testDonorDeleter = DependencyInjection.DependencyInjection.Provider.ResolveServiceOrThrow<ITestDonorDeleter>();
            await testDonorDeleter.DeleteDonors();
        }
    }
}