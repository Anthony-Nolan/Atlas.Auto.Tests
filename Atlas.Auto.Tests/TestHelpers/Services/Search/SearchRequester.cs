using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Requests;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.Search;

internal interface ISearchRequester
{
    /// <summary>
    /// The <paramref name="request"/> must be valid, i.e., not missing any required info.
    /// The request will be resent until it is successful or the maximum number of retries is reached.
    /// An invalid request will always be unsuccessful.
    /// </summary>
    Task<DebugResponse<SearchInitiationResponse>> SubmitValidSearchRequest(SearchRequest request);
}

internal class SearchRequester : ISearchRequester
{
    private readonly IDebugRequester debugRequester;
    private readonly IPublicApiFunctionsClient publicApiClient;

    public SearchRequester(IDebugRequester debugRequester, IPublicApiFunctionsClient publicApiClient)
    {
        this.debugRequester = debugRequester;
        this.publicApiClient = publicApiClient;
    }

    public async Task<DebugResponse<SearchInitiationResponse>> SubmitValidSearchRequest(SearchRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(5, 5, async () => await PostValidSearchRequest(request));
    }

    private async Task<DebugResponse<SearchInitiationResponse>> PostValidSearchRequest(SearchRequest request)
    {
        var response = await publicApiClient.PostSearchRequest(request);
        return new DebugResponse<SearchInitiationResponse>(response is { WasSuccess: true }, response.ResponseOnSuccess);
    }
}