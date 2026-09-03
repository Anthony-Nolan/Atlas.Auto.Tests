using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests;

[SetUpFixture]
public class TestsSetUp
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpForAllTests()
    {
        var provider = ServiceConfiguration.CreateProvider();
        var testDonorDeleter = provider.GetRequiredService<TestDonorDeleter>();
        await testDonorDeleter.DeleteDonors();
    }
}
