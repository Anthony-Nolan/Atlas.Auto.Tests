using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.ServiceBus;
using FluentAssertions;

namespace Atlas.Auto.Tests;

/// <summary>
/// This fixture is proof that the framework is in place and that an instance of Atlas can be reached for E2E testing.
/// todo: Delete this file once the real E2E tests have been added.
/// </summary>
[TestFixture]
internal class InitialTests
{
    private IDonorImportClient donorImportClient;
    private IMatchingAlgorithmClient matchingAlgorithmClient;
    private ISupportMessageClient supportMessageClient;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var provider = ServiceConfiguration.CreateProvider();
        donorImportClient = provider.ResolveServiceOrThrow<IDonorImportClient>();
        matchingAlgorithmClient = provider.ResolveServiceOrThrow<IMatchingAlgorithmClient>();
        supportMessageClient = provider.ResolveServiceOrThrow<ISupportMessageClient>();
    }

    [Test]
    public async Task DonorImport_CheckDonors_ReceivedDonorId()
    {
        var donorId = Guid.NewGuid().ToString();
        var result = await donorImportClient.CheckDonors(new[] { donorId });
        result.ReceivedDonors.Should().Contain(donorId);
    }

    [Test]
    public async Task MatchingAlgorithm_CheckDonors_ReceivedDonorId()
    {
        var donorId = Guid.NewGuid().ToString();
        var result = await matchingAlgorithmClient.CheckDonors(new[] { donorId });
        result.ReceivedDonors.Should().Contain(donorId);
    }

    [Test]
    public async Task SupportMessages_PeeksMessages()
    {
        var result = await supportMessageClient.PeekNotifications(new PeekServiceBusMessagesRequest { MessageCount = 1 });
        result.MessageCount.Should().BeGreaterOrEqualTo(0);
    }
}