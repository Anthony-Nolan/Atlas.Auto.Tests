using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Requests;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.Validation;

namespace Atlas.Auto.Tests.TestHelpers.Services.RepeatSearch;

internal interface IRepeatSearchRequester
{
    /// <summary>
    /// The <paramref name="request"/> must be valid, i.e., not missing any required info.
    /// The request will be resent until it is successful or the maximum number of retries is reached.
    /// An invalid request will always be deemed unsuccessful.
    /// </summary>
    Task<DebugResponse<SearchInitiationResponse>> SubmitValidSearchRequest(RepeatSearchRequest request);

    /// <summary>
    /// The <paramref name="request"/> must be **invalid** in terms of http request validation (excludes HLA validation).
    /// The request will be resent until "invalid request" response is received, or the maximum number of retries is reached.
    /// An valid request will always be deemed unsuccessful.
    /// </summary>
    Task<DebugResponse<IEnumerable<RequestValidationFailure>>> SubmitInvalidSearchRequest(RepeatSearchRequest request);
}

internal class RepeatSearchRequester : IRepeatSearchRequester
{
    private readonly IDebugRequester debugRequester;
    private readonly IPublicApiFunctionsClient publicApiClient;

    public RepeatSearchRequester(IDebugRequester debugRequester, IPublicApiFunctionsClient publicApiClient)
    {
        this.debugRequester = debugRequester;
        this.publicApiClient = publicApiClient;
    }

    public async Task<DebugResponse<SearchInitiationResponse>> SubmitValidSearchRequest(RepeatSearchRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(5, 5, async () => await PostValidSearchRequest(request));
    }

    public async Task<DebugResponse<IEnumerable<RequestValidationFailure>>> SubmitInvalidSearchRequest(RepeatSearchRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(5, 5, async () => await PostInvalidSearchRequest(request));
    }

    private async Task<DebugResponse<SearchInitiationResponse>> PostValidSearchRequest(RepeatSearchRequest request)
    {
        var response = await PostRequest(request);
        return new DebugResponse<SearchInitiationResponse>(
            response is { WasSuccess: true },
            response.ResponseOnSuccess);
    }

    private async Task<DebugResponse<IEnumerable<RequestValidationFailure>>> PostInvalidSearchRequest(RepeatSearchRequest request)
    {
        var response = await PostRequest(request);
        return new DebugResponse<IEnumerable<RequestValidationFailure>>(
            response is { WasSuccess: false },
            response.ValidationFailures);
    }

    private async Task<ResponseFromValidatedRequest<SearchInitiationResponse>> PostRequest(RepeatSearchRequest request)
        => await publicApiClient.PostRepeatSearchRequest(request);
}