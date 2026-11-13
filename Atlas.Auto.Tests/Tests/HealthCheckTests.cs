using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests.Tests;

[TestFixture]
[Category(nameof(HealthCheckTests))]
internal class HealthCheckTests : TestBase
{
    private const string RunningState = "Running";

    public HealthCheckTests() : base(nameof(HealthCheckTests))
    {
    }

    [Test]
    [Category("HealthCheck")]
    public async Task SqlServer_ShouldBeAccessible()
    {
        var factory = Provider.GetRequiredService<IDbContextFactory<AtlasContext>>();
        await using var ctx = factory.CreateDbContext();
        var canConnect = await ctx.Database.CanConnectAsync();
        canConnect.Should().BeTrue("SQL Server should be accessible");
    }

    [Test]
    [Category("HealthCheck")]
    public async Task ServiceBus_ShouldBeAccessible()
    {
        var sbClient = Provider.GetRequiredService<ServiceBusClient>();
        var sbSettings = Provider.GetRequiredService<ServiceBusSettings>();
        await using var receiver = sbClient.CreateReceiver(sbSettings.DonorImportResultsTopic, sbSettings.Subscription);
        var messages = await receiver.PeekMessagesAsync(1);
        messages.Should().NotBeNull("ServiceBus should be accessible");
    }

    [Test]
    [Category("HealthCheck")]
    public async Task BlobStorage_ShouldBeAccessible()
    {
        var blobClient = Provider.GetRequiredService<BlobServiceClient>();
        var properties = await blobClient.GetPropertiesAsync();
        properties.Should().NotBeNull("Blob Storage should be accessible");
    }

    [Test]
    [Category("HealthCheck")]
    public async Task AzureAuth_FunctionAppConfig_ShouldBeAccessible()
    {
        var helper = Provider.GetRequiredService<FunctionAppHelper>();
        var azureResource = Provider.GetRequiredService<AzureResourceSettings>();
        var appName = azureResource.FunctionApps["DonorImport"];
        var value = await helper.GetAppSetting(appName, "DonorImport:AllowFullModeImport");
        value.Should().NotBeNull("Function App config should be readable via Azure ARM");
    }

    /// <summary>
    /// A stopped function app makes its tests wait out every retry and fail much later; fail here instead.
    /// </summary>
    [Category("HealthCheck")]
    [TestCase("DonorImport")]
    [TestCase("Search")]
    [TestCase("RepeatSearch")]
    [TestCase("MatchingDonorManagement")]
    [TestCase("SearchTracking")]
    public async Task FunctionApp_ShouldBeRunning(string functionAppKey)
    {
        var helper = Provider.GetRequiredService<FunctionAppHelper>();
        var appName = Provider.GetRequiredService<AzureResourceSettings>().FunctionApps[functionAppKey];
        var pollyRetry = Provider.GetRequiredService<PollyRetry>();
        var retry = Provider.GetRequiredService<RetrySettings>();

        // Azure state: someone may have stopped the app
        var state = await helper.GetState(appName);
        state.Should().Be(RunningState, "Function app {0} should be running", appName);

        // Functions host: the app can be running while its host fails to start
        var hostRunning = await pollyRetry.WaitUntil(
            async () => await helper.GetRunningHostStartTime(appName) != null,
            retry.HealthCheck, $"Health check for {appName} host");
        hostRunning.Should().BeTrue("Functions host of {0} should be running", appName);
    }

    [Test]
    [Category("HealthCheck")]
    public async Task AzureAuth_AppInsights_ShouldBeAccessible()
    {
        var helper = Provider.GetRequiredService<AppInsightsHelper>();
        var failures = await helper.GetHlaExpansionFailures(daysToQuery: 1);
        failures.Should().NotBeNull("App Insights should be queryable via Azure credentials");
    }

    /// <summary>
    /// Only warns: the tests that need App Insights skip themselves, and the rest of the pipeline should still run.
    /// </summary>
    [Test]
    [Category("HealthCheck")]
    public async Task AppInsights_ShouldBeStoringTelemetry()
    {
        var problem = await Provider.GetRequiredService<AppInsightsIngestionCheck>().GetProblem();
        if (problem == null)
            return;

        // Shown as a warning on the Azure DevOps run
        TestContext.Progress.WriteLine($"##vso[task.logissue type=warning]{problem}");
        Assert.Warn(problem);
    }

    [Test]
    [Category("HealthCheck")]
    public async Task PublicApi_ShouldBeHealthy()
    {
        var client = Provider.GetRequiredService<PublicApiClient>();
        var pollyRetry = Provider.GetRequiredService<PollyRetry>();
        var retry = Provider.GetRequiredService<RetrySettings>();

        var result = await pollyRetry.ExecuteWithRetry(async () =>
        {
            var response = await client.HealthCheck();
            return response.Contains("Healthy") ? response : null;
        }, retry.HealthCheck, "Health check for PublicApi");
        result.Should().NotBeNull("PublicApi should be healthy");
    }
}
