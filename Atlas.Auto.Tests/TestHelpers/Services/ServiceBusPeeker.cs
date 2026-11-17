using Azure.Messaging.ServiceBus;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Peeks messages without taking them, so tests never change what Atlas or other subscribers see.
/// </summary>
internal static class ServiceBusPeeker
{
    private const int PeekBatchSize = 100;
    private const int MaxPeekIterations = 50;

    /// <summary>Most messages one peek returns; a queue that holds more is only partly read.</summary>
    public const int MaxMessages = PeekBatchSize * MaxPeekIterations;

    // Callers may pass times from a different clock (test machine, Event Grid), so start searching a bit earlier.
    private static readonly TimeSpan EnqueuedTimeTolerance = TimeSpan.FromMinutes(5);

    /// <param name="enqueuedSince">
    /// When set, peeking starts near the first message enqueued at that time instead of the oldest one,
    /// so messages are found however many older ones the subscription holds (e.g. long TTL audit subscriptions).
    /// Needs enqueued times that grow with sequence numbers, so not for dead-letter queues.
    /// </param>
    /// <returns>Messages in sequence order, at most <see cref="MaxMessages"/>.</returns>
    public static async Task<IReadOnlyList<ServiceBusReceivedMessage>> PeekMessages(
        ServiceBusReceiver receiver, DateTimeOffset? enqueuedSince = null)
    {
        var messages = new List<ServiceBusReceivedMessage>();

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

            messages.AddRange(batch);
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
