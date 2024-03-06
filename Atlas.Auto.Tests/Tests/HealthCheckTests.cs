using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Debug.Client.Clients;
using FluentAssertions;

namespace Atlas.Auto.Tests.Tests;

/// <summary>
/// This fixture is confirmation that the instance of Atlas under test can be reached for E2E testing.
/// </summary>
[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
internal class HealthCheckTests : TestBase
{
    private static object[] clientsToTest = {
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
        dynamic healthChecker = Provider.ResolveServiceOrThrow(typeof(IHealthChecker<>).MakeGenericType(clientType));
        var test = BuildTestLogger(action);

        test.LogStart(action);
        var result = await healthChecker.HealthCheck() as bool?;
        test.AssertThenLogAndThrow( () =>
        {
            result.Should().NotBeNull();
            result!.Value.Should().BeTrue();
        }, action);
    }
}