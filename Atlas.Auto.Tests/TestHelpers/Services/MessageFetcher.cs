using Atlas.Debug.Client.Models.ServiceBus;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal interface IMessageFetcher
{
    Task<IEnumerable<TMessage>> FetchAllMessages<TMessage>(
        Func<PeekServiceBusMessagesRequest, Task<PeekServiceBusMessagesResponse<TMessage>>> peekFunc) where TMessage : class;
}

internal class MessageFetcher :IMessageFetcher
{
    public async Task<IEnumerable<TMessage>> FetchAllMessages<TMessage>(
        Func<PeekServiceBusMessagesRequest, Task<PeekServiceBusMessagesResponse<TMessage>>> peekFunc) where TMessage : class
    {
        // todo #7: fetch all messages from new method param `fetchFromSequenceNumber`
        // to avoid having to cycle through whole queue on subsequent calls

        var messages = new List<TMessage>();
        const int batchSize = 5;
        long lastSequenceNumber = -1;

        while (true)
        {
            var peekResponse = await peekFunc(new PeekServiceBusMessagesRequest
            {
                FromSequenceNumber = lastSequenceNumber + 1,
                MessageCount = batchSize
            });

            var lastMessageCount = peekResponse.MessageCount;
            lastSequenceNumber = (long)peekResponse.LastSequenceNumber;
            messages.AddRange(peekResponse.PeekedMessages);

            if (lastMessageCount < batchSize) break;
        }

        return messages;
    }
}