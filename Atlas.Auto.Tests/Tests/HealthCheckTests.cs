using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Debug.Client.Clients;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests.Tests;

[TestFixture]
[Category(nameof(HealthCheckTests))]
internal class HealthCheckTests : TestBase
{
    private static readonly object[] clientsToTest = {
        typeof(IDonorImportFunctionsClient),
        typeof(IMatchingAlgorithmFunctionsClient),
        typeof(IPublicApiFunctionsClient),
        typeof(ITopLevelFunctionsClient),
        typeof(IRepeatSearchFunctionsClient)
    };

    public HealthCheckTests() : base(nameof(HealthCheckTests))
    {
    }

    [Category("HealthCheck")]
    [TestCaseSource(nameof(clientsToTest))]
    public async Task HealthCheck(Type clientType)
    {
        var action = $"Health Check Test for {clientType.Name}";
        var client = (ICommonAtlasFunctions)Provider.GetRequiredService(clientType);
        var pollyRetry = Provider.GetRequiredService<PollyRetry>();
        var retry = Provider.GetRequiredService<RetrySettings>();
        var test = BuildTestLogger(action);

        test.LogStart(action);
        var result = await pollyRetry.ExecuteWithRetry(async () =>
        {
            var response = await client.HealthCheck();
            return response?.Contains("Healthy") == true ? response : null;
        }, retry.HealthCheck, $"Health check for {clientType.Name}");
        result.Should().NotBeNull("{0} should be healthy", clientType.Name);
        test.LogCompletion(action);
    }
}
