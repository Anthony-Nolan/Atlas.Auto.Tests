using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Client.Models.Search.Results.Matching;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Reads the subscription the Search function (match prediction orchestration) takes matching results from: a message
/// stays there until the function has processed it, and goes to its dead-letter queue if the function fails on it.
/// </summary>
internal class MatchPredictionSubscriptionReader(ServiceBusClient serviceBusClient, ServiceBusSettings settings)
{
    /// <summary>Topic and subscription, for messages.</summary>
    public string Subscription => $"{settings.MatchingResultsTopic}/{settings.MatchPredictionSubscription}";

    /// <summary>The matching notification of the search is still in the subscription: not yet processed.</summary>
    /// <param name="enqueuedSinceUtc">When the search was sent: the notification cannot be older.</param>
    public async Task<bool> HasMatchingNotification(string searchRequestId, DateTime enqueuedSinceUtc)
    {
        await using var receiver = serviceBusClient.CreateReceiver(
            settings.MatchingResultsTopic, settings.MatchPredictionSubscription);
        var messages = await ServiceBusPeeker.PeekMessages(receiver, new DateTimeOffset(enqueuedSinceUtc, TimeSpan.Zero));
        return HasNotificationOf(messages, Subscription, searchRequestId);
    }

    /// <summary>The Search function failed on the matching notification of the search and dead-lettered it.</summary>
    public async Task<bool> HasDeadLetteredMatchingNotification(string searchRequestId)
    {
        await using var receiver = serviceBusClient.CreateReceiver(
            settings.MatchingResultsTopic,
            settings.MatchPredictionSubscription,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        // Dead-lettered messages keep their original enqueued time, so the queue cannot be searched by time: read it all
        var messages = await ServiceBusPeeker.PeekMessages(receiver);
        return HasNotificationOf(messages, $"{Subscription} dead-letter queue", searchRequestId);
    }

    private static bool HasNotificationOf(
        IReadOnlyCollection<ServiceBusReceivedMessage> messages, string source, string searchRequestId)
    {
        // A partly read queue may hide the notification, and "not there" would pass a check by mistake
        ServiceBusPeeker.ShouldHaveReadAll(messages, source, $"the matching notification of search {searchRequestId}");
        return messages.Any(m => SearchRequestIdOf(m) == searchRequestId);
    }

    private static string? SearchRequestIdOf(ServiceBusReceivedMessage message)
    {
        try
        {
            return JsonConvert.DeserializeObject<MatchingResultsNotification>(message.Body.ToString())?.SearchRequestId;
        }
        catch (JsonException)
        {
            // A dead-letter queue may hold a body nobody could read: it is not a notification of this search
            return null;
        }
    }
}
