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
internal class HealthCheckTests : TestBase
{
    private static object[] clientsToTest = {
        typeof(IDonorImportFunctionsClient),
        typeof(IMatchingAlgorithmFunctionsClient),
        typeof(ITopLevelFunctionsClient)
    };

    private static string testName = "Health Check Test";

    public HealthCheckTests()
    {
        ExtentTest = ExtentManager.CreateTest(testName);
    }

    [Category("HealthCheck")]
    [TestCaseSource(nameof(clientsToTest))]
    public async Task HealthCheck(Type clientType)
    {
        var test = ExtentManager.CreateMethod(testName,
            $"Health Check Test for {clientType.Name}",
            $"Health Check Tests for {clientType.Name}");

        test.Log(Status.Info, $"Start Health Check Test for client type: {clientType.Name}");
        var client = Provider.ResolveServiceOrThrow(clientType) as HttpFunctionClient;
        var result = await client!.HealthCheck();
        test.Log(Status.Info, $"Health Check Test for client type: {clientType.Name} result: {result}");
        result.Should().Contain("successful");
    }
}