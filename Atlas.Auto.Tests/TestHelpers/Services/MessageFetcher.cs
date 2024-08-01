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
        const int batchSize = 100;
        long lastSequenceNumber = 0;

        while (true)
        {
            var peekResponse = await peekFunc(new PeekServiceBusMessagesRequest
            {
                FromSequenceNumber = lastSequenceNumber,
                MessageCount = batchSize
            });

            var lastMessageCount = peekResponse.MessageCount;
            lastSequenceNumber += lastMessageCount;
            messages.AddRange(peekResponse.PeekedMessages);

            if (lastMessageCount < batchSize) break;
        }

        return messages;
    }
}