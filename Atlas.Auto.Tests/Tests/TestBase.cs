using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Utils.Reporting;
using AventStack.ExtentReports;

namespace Atlas.Auto.Tests.Tests;

public abstract class TestBase
{
    private static ExtentTest ExtentTestForFixture;
    protected IServiceProvider Provider;

    protected TestBase(string testFixtureName)
    {
        ExtentTestForFixture = ExtentManager.CreateForFixture(testFixtureName);
        Provider = ServiceConfiguration.CreateProvider();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        ExtentTestForFixture.Extent.Flush();
    }
}