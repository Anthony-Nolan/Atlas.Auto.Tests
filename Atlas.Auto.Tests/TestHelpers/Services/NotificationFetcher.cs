using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class NotificationFetcher<TNotification> where TNotification : class
{
    private const int PeekBatchSize = 100;
    private const int MaxPeekIterations = 50;

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

    public async Task<TNotification?> FetchNotification(Func<TNotification, bool> filter, string searchDescription)
    {
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var messages = await PeekAllMessages();
            return messages.LastOrDefault(filter);
        }, _retryPolicy, $"{_operationName}: looking for {searchDescription}");
    }

    private async Task<List<TNotification>> PeekAllMessages()
    {
        var messages = new List<TNotification>();

        await using var receiver = _serviceBusClient.CreateReceiver(_topicName, _subscriptionName);

        long? fromSequenceNumber = null;

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
}
