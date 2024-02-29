using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services.Search;
using Atlas.Client.Models.Search.Requests;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

/// <summary>
/// Workflow to execute a search request and determine the outcome.
/// </summary>
internal interface ISearchWorkflow
{
    Task<DebugResponse<SearchInitiationResponse>> SubmitSearchRequest(SearchRequest request);
}

internal class SearchWorkflow : ISearchWorkflow
{
    private readonly ISearchRequester searchRequester;
    public SearchWorkflow(
        ISearchRequester searchRequester)
    {
        this.searchRequester = searchRequester;
    }

    public async Task<DebugResponse<SearchInitiationResponse>> SubmitSearchRequest(SearchRequest request)
    {
        return await searchRequester.SubmitValidSearchRequest(request);
    }
}