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
        Func<PeekServiceBusMessagesRequest, Task<PeekServiceBusMessagesResponse<TMessage>>> peekFunc)
        where TMessage : class
    {
        const int batchSize = 100;


        var peekResponse = await peekFunc(new PeekServiceBusMessagesRequest
        {
            FromSequenceNumber = 0,
            MessageCount = batchSize
        });

        return peekResponse.PeekedMessages;
    }
}