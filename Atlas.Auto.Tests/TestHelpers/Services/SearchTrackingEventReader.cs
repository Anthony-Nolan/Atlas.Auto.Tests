using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.Messaging.ServiceBus;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Reads the search-tracking-events messages of one search: the copies in the audit subscription,
/// and the ones the search tracking function dead-lettered.
/// </summary>
internal class SearchTrackingEventReader(ServiceBusClient serviceBusClient, ServiceBusSettings settings)
{
    /// <param name="searchIdentifier">Search or repeat search ID.</param>
    /// <param name="requestedAt">When the search was requested: the audit subscription holds a day of messages.</param>
    public async Task<IReadOnlyCollection<SearchTrackingEventMessage>> GetAuditedEvents(
        string searchIdentifier, DateTimeOffset requestedAt)
    {
        await using var receiver = serviceBusClient.CreateReceiver(
            settings.SearchTrackingEventsTopic, settings.AuditSubscription);
        var messages = await ServiceBusPeeker.PeekMessages(receiver, requestedAt);
        return EventsOf(messages, searchIdentifier);
    }

    /// <param name="searchIdentifier">Search or repeat search ID.</param>
    public async Task<IReadOnlyCollection<SearchTrackingEventMessage>> GetDeadLetteredEvents(string searchIdentifier)
    {
        await using var receiver = serviceBusClient.CreateReceiver(
            settings.SearchTrackingEventsTopic,
            settings.SearchTrackingSubscription,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        // Dead-lettered messages keep their original enqueued time, so the queue cannot be searched by time: read it all
        var messages = await ServiceBusPeeker.PeekMessages(receiver);
        if (messages.Count >= ServiceBusPeeker.MaxMessages)
        {
            throw new InvalidOperationException(
                $"The dead-letter queue of {settings.SearchTrackingEventsTopic}/{settings.SearchTrackingSubscription} " +
                $"holds more than {ServiceBusPeeker.MaxMessages} messages, so the test cannot tell whether search " +
                $"{searchIdentifier} has any there. Clear the queue on this environment.");
        }

        return EventsOf(messages, searchIdentifier);
    }

    private static List<SearchTrackingEventMessage> EventsOf(
        IEnumerable<ServiceBusReceivedMessage> messages, string searchIdentifier)
    {
        var identifier = searchIdentifier.ToLowerInvariant();
        return messages
            .Select(SearchTrackingEventMessage.From)
            .Where(e => e.SearchIdentifier == identifier)
            .ToList();
    }
}
