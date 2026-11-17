using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class NotificationFetcher<TNotification> where TNotification : class
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly string _topicName;
    private readonly string _subscriptionName;
    private readonly PollyRetry _pollyRetry;
    private readonly RetryPolicy _retryPolicy;
    private readonly string _operationName;

    public NotificationFetcher(
        ServiceBusClient serviceBusClient,
        string topicName,
        string subscriptionName,
        PollyRetry pollyRetry,
        RetryPolicy retryPolicy,
        string operationName)
    {
        _serviceBusClient = serviceBusClient;
        _topicName = topicName;
        _subscriptionName = subscriptionName;
        _pollyRetry = pollyRetry;
        _retryPolicy = retryPolicy;
        _operationName = operationName;
    }

    /// <param name="enqueuedSince">See <see cref="ServiceBusPeeker.PeekMessages"/>.</param>
    public async Task<TNotification?> FetchNotification(
        Func<TNotification, bool> filter, string searchDescription, DateTimeOffset? enqueuedSince = null)
    {
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var messages = await PeekAllMessages(enqueuedSince);
            return messages.LastOrDefault(filter);
        }, _retryPolicy, $"{_operationName}: looking for {searchDescription}");
    }

    /// <summary>
    /// Retries until at least <paramref name="expectedCount"/> messages match, then returns all matches,
    /// so the caller can assert on the exact count.
    /// </summary>
    /// <param name="enqueuedSince">See <see cref="ServiceBusPeeker.PeekMessages"/>.</param>
    public async Task<IReadOnlyCollection<TNotification>?> FetchNotifications(
        Func<TNotification, bool> filter, int expectedCount, string searchDescription, DateTimeOffset? enqueuedSince = null)
    {
        return await _pollyRetry.ExecuteWithRetry<IReadOnlyCollection<TNotification>>(async () =>
        {
            var messages = await PeekAllMessages(enqueuedSince);
            var matching = messages.Where(filter).ToList();
            return matching.Count >= expectedCount ? matching : null;
        }, _retryPolicy, $"{_operationName}: looking for {expectedCount} {searchDescription}");
    }

    private async Task<List<TNotification>> PeekAllMessages(DateTimeOffset? enqueuedSince)
    {
        await using var receiver = _serviceBusClient.CreateReceiver(_topicName, _subscriptionName);
        var messages = await ServiceBusPeeker.PeekMessages(receiver, enqueuedSince);

        return messages
            .Select(m => JsonConvert.DeserializeObject<TNotification>(m.Body.ToString()))
            .OfType<TNotification>()
            .ToList();
    }
}
