using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class NotificationFetcher<TNotification> where TNotification : class
{
    private const int PeekBatchSize = 100;
    private const int MaxPeekIterations = 50;

    // Callers may pass times from a different clock (test machine, Event Grid), so start searching a bit earlier.
    private static readonly TimeSpan EnqueuedTimeTolerance = TimeSpan.FromMinutes(5);

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

    /// <param name="enqueuedSince">
    /// When set, peeking starts near the first message enqueued at that time instead of the oldest one,
    /// so messages are found however many older ones the subscription holds (e.g. long TTL audit subscriptions).
    /// </param>
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
    /// <param name="enqueuedSince">See <see cref="FetchNotification"/>.</param>
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
        var messages = new List<TNotification>();

        await using var receiver = _serviceBusClient.CreateReceiver(_topicName, _subscriptionName);

        var fromSequenceNumber = enqueuedSince.HasValue
            ? await FindSequenceNumberEnqueuedSince(receiver, enqueuedSince.Value - EnqueuedTimeTolerance)
            : null;

        for (var i = 0; i < MaxPeekIterations; i++)
        {
            var batch = fromSequenceNumber.HasValue
                ? await receiver.PeekMessagesAsync(PeekBatchSize, fromSequenceNumber.Value)
                : await receiver.PeekMessagesAsync(PeekBatchSize);

            if (batch.Count == 0)
                break;

            foreach (var msg in batch)
            {
                var body = msg.Body.ToString();
                var parsed = JsonConvert.DeserializeObject<TNotification>(body);
                if (parsed != null)
                    messages.Add(parsed);
            }

            fromSequenceNumber = batch[^1].SequenceNumber + 1;

            if (batch.Count < PeekBatchSize)
                break;
        }

        return messages;
    }

    /// <summary>
    /// Service Bus can only peek forwards from a sequence number, and peeking one message from N returns the first
    /// message with sequence number >= N. Sequence numbers and enqueued times both grow (the namespace is not partitioned),
    /// so we can binary search for the first message enqueued since <paramref name="since"/>: ~2·log2(N) single-message peeks.
    /// </summary>
    /// <returns>Sequence number to start peeking from, or null if the subscription is empty.</returns>
    private static async Task<long?> FindSequenceNumberEnqueuedSince(ServiceBusReceiver receiver, DateTimeOffset since)
    {
        var oldest = await PeekOne(receiver, fromSequenceNumber: null);
        if (oldest == null)
            return null;
        if (oldest.EnqueuedTime >= since)
            return oldest.SequenceNumber;

        // Invariant: lo is a message enqueued before `since`;
        // peeking from hi gives nothing or a message enqueued since `since`.
        var lo = oldest.SequenceNumber;
        long step = 1;
        long hi;
        while (true)
        {
            var probe = await PeekOne(receiver, lo + step);
            if (probe == null || probe.EnqueuedTime >= since)
            {
                hi = lo + step;
                break;
            }
            lo = probe.SequenceNumber;
            step *= 2;
        }

        while (hi - lo > 1)
        {
            var mid = lo + (hi - lo) / 2;
            var probe = await PeekOne(receiver, mid);
            if (probe == null || probe.EnqueuedTime >= since)
                hi = mid;
            else
                lo = probe.SequenceNumber;
        }

        return lo + 1;
    }

    private static async Task<ServiceBusReceivedMessage?> PeekOne(ServiceBusReceiver receiver, long? fromSequenceNumber)
    {
        var batch = fromSequenceNumber.HasValue
            ? await receiver.PeekMessagesAsync(1, fromSequenceNumber.Value)
            : await receiver.PeekMessagesAsync(1);
        return batch.Count > 0 ? batch[0] : null;
    }
}
