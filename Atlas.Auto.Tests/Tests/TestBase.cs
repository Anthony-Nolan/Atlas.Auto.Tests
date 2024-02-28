using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Utils.Reporting;
using AventStack.ExtentReports;

namespace Atlas.Auto.Tests.Tests;

public abstract class TestBase
{
    protected IServiceProvider Provider;
    protected static string TestFixtureName;
    private static ExtentTest ExtentTestForFixture;

    protected TestBase(string testFixtureName)
    {
        Provider = ServiceConfiguration.CreateProvider();
        TestFixtureName = testFixtureName;
        ExtentTestForFixture = ExtentManager.CreateForFixture(testFixtureName);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        ExtentTestForFixture.Extent.Flush();
    }
}