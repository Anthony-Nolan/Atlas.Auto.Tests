using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Utils.Reporting;
using Atlas.Debug.Client.Clients;
using AventStack.ExtentReports;
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
        typeof(ITopLevelFunctionsClient)
    };

    public HealthCheckTests() : base(nameof(HealthCheckTests))
    {
    }

    [Category("HealthCheck")]
    [TestCaseSource(nameof(clientsToTest))]
    public async Task HealthCheck(Type clientType)
    {
        dynamic healthChecker = Provider.ResolveServiceOrThrow(typeof(IHealthChecker<>).MakeGenericType(clientType));
        var test = ExtentManager.CreateForTest(TestFixtureName, $"Health Check Test for {clientType.Name}");

        test.Log(Status.Info, $"Started health check for client type {clientType.Name}");
        var result = await healthChecker.HealthCheck() as bool?;
        test.Log(result != null && result.Value ? Status.Pass : Status.Fail, $"Health check result for {clientType.Name}: {result}");
        result.Should().BeTrue();
    }
}