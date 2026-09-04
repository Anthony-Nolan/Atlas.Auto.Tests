using Microsoft.Extensions.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.TestHelpers.TestSteps;

namespace Atlas.Auto.Tests.Tests.Search;

internal abstract class SearchTestBase : TestBase
{
    protected SearchTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected static IEnumerable<TestCaseData> Cases()
    {
        yield return new TestCaseData(null).SetName("{m}" + " (default)");
        yield return new TestCaseData(true).SetName("{m}" + " (new)");
        yield return new TestCaseData(false).SetName("{m}" + " (old)");
    }

    protected static async Task ExecuteWithRetry(Func<Task> action)
    {
        var pollyRetry = Provider.GetRequiredService<PollyRetry>();
        var retry = Provider.GetRequiredService<RetrySettings>();
        await pollyRetry.ExecuteWithRetry(action, retry.TestRetry, "Test-level retry");
    }

    protected SearchTestSteps GetSearchTestSteps(string testName)
    {
        var testLogger = BuildTestLogger(testName);
        var importSteps = ResolveDonorImportStepsForSearchTests(testLogger);
        return new SearchTestSteps(Provider, importSteps, testLogger, testName);
    }

    private DonorImportStepsForSearchTests ResolveDonorImportStepsForSearchTests(ITestLogger testLogger)
    {
        var donorImportTestSteps = new DonorImportTestSteps(Provider, testLogger);
        return new DonorImportStepsForSearchTests(donorImportTestSteps, testLogger);
    }
}
