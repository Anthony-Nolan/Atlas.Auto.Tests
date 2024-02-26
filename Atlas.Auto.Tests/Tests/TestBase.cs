using Atlas.Auto.Tests.DependencyInjection;
using AventStack.ExtentReports;

namespace Atlas.Auto.Tests.Tests
{
    public class TestBase
    {
        public static ExtentTest ExtentTest;
        public IServiceProvider Provider;

        static TestBase()
        {

        }

        protected TestBase()
        {
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Provider = ServiceConfiguration.CreateProvider();
        }

        /// <summary>
        /// This should be overloaded in derived classes.
        /// Initialization of state before each test case runs. Equals [SetUp]
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// This should be overloaded in derived classes.
        /// Uninitialization of state before each test case runs. Equals [TearDown]
        /// </summary>
        protected virtual void Uninitialize()
        {
        }

        [SetUp]
        public void InitializeBase()
        {
            Initialize();
        }

        [TearDown]
        public void UninitializeBase()
        {
            Uninitialize();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            ExtentTest.Extent.Flush();
        }
    }
}
