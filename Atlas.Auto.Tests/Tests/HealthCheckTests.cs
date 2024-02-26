using Atlas.Auto.Tests.DependencyInjection;
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
internal class HealthCheckTests
{
    private static object[] _clientsToTest = {
        typeof(IDonorImportFunctionsClient),
        typeof(IMatchingAlgorithmFunctionsClient),
        typeof(ITopLevelFunctionsClient)
    };

    public HealthCheckTests()
    {
        ExtentTest = ExtentManager.CreateTest("Health Check Tests", "Health Check Tests");
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Provider = ServiceConfiguration.CreateProvider();
    }

    [Category("HealthCheck")]
    [TestCaseSource(nameof(_clientsToTest))]
    public async Task HealthCheck(Type clientType)
    {
        var test = ExtentManager.CreateMethod($"Health Check Test for {clientType.Name}",
            $"Health Check Test for {clientType.Name}",
            $"Health Check Tests for {clientType.Name}");

        test.Log(Status.Info, $"Start Health Check Test for client type: {clientType.Name}");
        var client = Provider.ResolveServiceOrThrow(clientType) as HttpFunctionClient;
        var result = await client!.HealthCheck();
        test.Log(Status.Info, $"Health Check Test for client type: {clientType.Name} result: {result}");
        result.Should().Contain("successful");
    }
}