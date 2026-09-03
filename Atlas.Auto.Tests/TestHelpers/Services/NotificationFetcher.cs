using Atlas.Debug.Client.Models.ServiceBus;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class NotificationFetcher<TNotification> where TNotification : class
{
    private const int PeekBatchSize = 100;
    private const int MaxPeekIterations = 50;

    private readonly Func<PeekServiceBusMessagesRequest, Task<PeekServiceBusMessagesResponse<TNotification>>> _peekFunc;
    private readonly int _retryCount;
    private readonly int _retryIntervalInSeconds;
    private readonly string _operationName;

    public NotificationFetcher(
        Func<PeekServiceBusMessagesRequest, Task<PeekServiceBusMessagesResponse<TNotification>>> peekFunc,
        int retryCount,
        int retryIntervalInSeconds,
        string operationName)
    {
        _peekFunc = peekFunc;
        _retryCount = retryCount;
        _retryIntervalInSeconds = retryIntervalInSeconds;
        _operationName = operationName;
    }

    public async Task<TNotification?> FetchNotification(Func<TNotification, bool> filter)
    {
        return await PollyRetry.ExecuteWithRetry(async () =>
        {
            var messages = await PeekAllMessages();
            return messages.LastOrDefault(filter);
        }, _retryCount, _retryIntervalInSeconds, _operationName);
    }

    private async Task<List<TNotification>> PeekAllMessages()
    {
        var messages = new List<TNotification>();
        long fromSequenceNumber = 0;

        for (var i = 0; i < MaxPeekIterations; i++)
        {
            var response = await _peekFunc(new PeekServiceBusMessagesRequest
            {
                FromSequenceNumber = fromSequenceNumber,
                MessageCount = PeekBatchSize
            });

            messages.AddRange(response.PeekedMessages);

            if (response.MessageCount < PeekBatchSize)
                break;

            fromSequenceNumber = (long)(response.LastSequenceNumber! + 1);
        }

        return messages;
    }
}
