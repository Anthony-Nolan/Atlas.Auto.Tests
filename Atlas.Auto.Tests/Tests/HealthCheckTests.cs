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

    [Test]
    [Category("HealthCheck")]
    public async Task AzureAuth_AppInsights_ShouldBeAccessible()
    {
        var helper = Provider.GetRequiredService<AppInsightsHelper>();
        var failures = await helper.GetHlaExpansionFailures(daysToQuery: 1);
        failures.Should().NotBeNull("App Insights should be queryable via Azure credentials");
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
