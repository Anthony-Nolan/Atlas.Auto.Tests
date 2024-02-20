using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Debug.Client.Clients;
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

    private IServiceProvider provider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        provider = ServiceConfiguration.CreateProvider();
    }

    [Category("HealthCheck")]
    [TestCaseSource(nameof(_clientsToTest))]
    public async Task HealthCheck(Type clientType)
    {
        var client = provider.ResolveServiceOrThrow(clientType) as HttpFunctionClient;
        var result = await client!.HealthCheck();
        result.Should().Contain("successful");
    }
}