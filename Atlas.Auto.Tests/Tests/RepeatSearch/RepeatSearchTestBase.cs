using Microsoft.Extensions.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.Tests.Search;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

internal abstract class RepeatSearchTestBase : SearchTestBase
{
    protected RepeatSearchTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected RepeatSearchTestSteps GetRepeatSearchTestSteps(string testName)
    {
        var searchTestSteps = GetSearchTestSteps(testName);

        return new RepeatSearchTestSteps(
            Provider.GetRequiredService<IPublicApiFunctionsClient>(),
            Provider.GetRequiredService<IRepeatSearchFunctionsClient>(),
            Provider.GetRequiredService<ITopLevelFunctionsClient>(),
            Provider.GetRequiredService<PollyRetry>(),
            Provider.GetRequiredService<RetrySettings>(),
            searchTestSteps,
            ResolveDonorImportStepsForSearchTests(searchTestSteps.Logger),
            searchTestSteps.Logger,
            testName);
    }
}
