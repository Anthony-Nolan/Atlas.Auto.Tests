using Atlas.Auto.Tests.DependencyInjection;

namespace Atlas.Auto.Tests
{
    [SetUpFixture]
    public class TestsSetUp
    {
        [OneTimeSetUp]
        public void OneTimeSetUpForAllTests()
        {
            DependencyInjection.DependencyInjection.Provider = ServiceConfiguration.CreateProvider();
        }
    }
}