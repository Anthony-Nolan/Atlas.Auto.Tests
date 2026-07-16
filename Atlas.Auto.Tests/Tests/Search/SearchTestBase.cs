using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Polly;
using Polly.Retry;

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
        var retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                ShouldHandle = new PredicateBuilder().Handle<Exception>()
            })
            .Build();

        await retryPipeline.ExecuteAsync(async _ => await action(), CancellationToken.None);
    }

    protected TestServices<ISearchTestSteps> GetSearchTestServices(string testName)
    {
        var testLogger = BuildTestLogger(testName);
        var importStepsForSearchTests = ResolveDonorImportStepsForSearchTests(testLogger);
        return ResolveSearchTestServices(importStepsForSearchTests, testLogger, testName);
    }

    protected DonorImportStepsForSearchTests ResolveDonorImportStepsForSearchTests(ITestLogger testLogger)
    {
        var donorImportWorkflow = Provider.ResolveServiceOrThrow<IDonorImportWorkflow>();
        var donorImportTestSteps = new DonorImportTestSteps(donorImportWorkflow, testLogger);
        return new DonorImportStepsForSearchTests(donorImportTestSteps, testLogger);
    }

    private TestServices<ISearchTestSteps> ResolveSearchTestServices(
        IDonorImportStepsForSearchTests importStepsForSearchTests,
        ITestLogger testLogger,
        string testName)
    {
        var searchWorkflow = Provider.ResolveServiceOrThrow<ISearchWorkflow>();
        var searchTestSteps = new SearchTestSteps(searchWorkflow, importStepsForSearchTests, testLogger, testName);
        return new TestServices<ISearchTestSteps>(searchTestSteps, testLogger);
    }
}