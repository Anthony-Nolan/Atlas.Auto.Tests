using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Utils.Reporting;
using AventStack.ExtentReports;

namespace Atlas.Auto.Tests.Tests;

internal abstract class TestBase
{
    protected readonly IServiceProvider Provider;
    protected readonly string TestFixtureName;
    private readonly ExtentTest extentTestForFixture;

    protected TestBase(string testFixtureName)
    {
        Provider = ServiceConfiguration.CreateProvider();
        TestFixtureName = testFixtureName;
        extentTestForFixture = ExtentManager.CreateForFixture(testFixtureName);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        extentTestForFixture.Extent.Flush();
    }

    protected ITestLogger BuildTestLogger(string testName) => new TestLogger(ExtentManager.CreateForTest(TestFixtureName, testName));
}