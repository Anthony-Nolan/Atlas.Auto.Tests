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
        long lastSequenceNumber = -1;
        int lastMessageCount;

        do
        {
            var peekResponse = await peekFunc(new PeekServiceBusMessagesRequest
            {
                FromSequenceNumber = ++lastSequenceNumber,
                MessageCount = 100
            });

            lastMessageCount = peekResponse.MessageCount;
            if (lastMessageCount == 0) break;

            messages.AddRange(peekResponse.PeekedMessages);
        } while (lastMessageCount > 0);

        return messages;
    }
}