using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.Tests;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Interfaces;

namespace Atlas.Auto.Tests.TestHelpers.Attributes;

/// <summary>
/// Marks a test that reads telemetry from App Insights. The test is skipped, before it does any work, when App Insights
/// is not storing telemetry (see <see cref="AppInsightsIngestionCheck"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
internal class RequiresAppInsightsAttribute : NUnitAttribute, ITestAction
{
    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test)
    {
        var problem = TestBase.Provider.GetRequiredService<AppInsightsIngestionCheck>().GetProblem().GetAwaiter().GetResult();
        if (problem != null)
            Assert.Ignore(problem);
    }

    public void AfterTest(ITest test)
    {
    }
}
