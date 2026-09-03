using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Auto.Utils.Reporting;
using AventStack.ExtentReports;

namespace Atlas.Auto.Tests.Tests;

[Parallelizable(ParallelScope.All)]
internal abstract class TestBase
{
    private static readonly Lazy<IServiceProvider> LazyProvider = new(ServiceConfiguration.CreateProvider);
    protected static IServiceProvider Provider => LazyProvider.Value;

    protected readonly string TestFixtureName;
    private readonly ExtentTest _extentTestForFixture;

    protected TestBase(string testFixtureName)
    {
        TestFixtureName = testFixtureName;
        _extentTestForFixture = ExtentManager.CreateForFixture(testFixtureName);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _extentTestForFixture.Extent.Flush();
    }

    protected ITestLogger BuildTestLogger(string testName) =>
        new TestLogger(ExtentManager.CreateForTest(TestFixtureName, testName));
}
