using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Debug.Client.Models.ServiceBus;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class NotificationFetcher<TNotification> where TNotification : class
{
    private const int PeekBatchSize = 100;
    private const int MaxPeekIterations = 50;

    private readonly Func<PeekServiceBusMessagesRequest, Task<PeekServiceBusMessagesResponse<TNotification>>> _peekFunc;
    private readonly PollyRetry _pollyRetry;
    private readonly RetryPolicy _retryPolicy;
    private readonly string _operationName;

    public NotificationFetcher(
        Func<PeekServiceBusMessagesRequest, Task<PeekServiceBusMessagesResponse<TNotification>>> peekFunc,
        PollyRetry pollyRetry,
        RetryPolicy retryPolicy,
        string operationName)
    {
        _peekFunc = peekFunc;
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
