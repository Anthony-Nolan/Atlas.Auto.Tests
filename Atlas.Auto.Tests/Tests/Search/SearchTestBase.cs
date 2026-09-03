using Microsoft.Extensions.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Debug.Client.Clients;

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

        return new SearchTestSteps(
            Provider.GetRequiredService<IPublicApiFunctionsClient>(),
            Provider.GetRequiredService<IMatchingAlgorithmFunctionsClient>(),
            Provider.GetRequiredService<ITopLevelFunctionsClient>(),
            Provider.GetRequiredService<PollyRetry>(),
            Provider.GetRequiredService<RetrySettings>(),
            importSteps,
            testLogger,
            testName);
    }

    protected DonorImportStepsForSearchTests ResolveDonorImportStepsForSearchTests(ITestLogger testLogger)
    {
        var donorImportWorkflow = Provider.GetRequiredService<DonorImportWorkflow>();
        var donorImportTestSteps = new DonorImportTestSteps(donorImportWorkflow, testLogger);
        return new DonorImportStepsForSearchTests(donorImportTestSteps, testLogger);
    }
}
