using System.Net;
using Atlas.Client.Models.Scoring.Requests;
using Atlas.Client.Models.Scoring.Results;
using Atlas.Client.Models.Search.Requests;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class PublicApiClient(HttpClient httpClient)
{
    public async Task<SearchResponse> PostSearchRequest(SearchRequest request)
    {
        return await PostValidatedRequest<SearchRequest, SearchInitiationResponse>("Search", request);
    }

    public async Task<SearchResponse> PostRepeatSearchRequest(RepeatSearchRequest request)
    {
        return await PostValidatedRequest<RepeatSearchRequest, SearchInitiationResponse>("RepeatSearch", request);
    }

    public async Task<ScoringResult> PostScore(DonorHlaScoringRequest request)
    {
        return await PostRequest<DonorHlaScoringRequest, ScoringResult>("Score", request);
    }

    public async Task<List<DonorScoringResult>> PostScoreBatch(DonorHlaBatchScoringRequest request)
    {
        return await PostRequest<DonorHlaBatchScoringRequest, List<DonorScoringResult>>("ScoreBatch", request);
    }

    public async Task<string> HealthCheck()
    {
        var response = await httpClient.GetAsync("HealthCheck");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<TResponse> PostRequest<TBody, TResponse>(string endpoint, TBody body)
    {
        var json = JsonConvert.SerializeObject(body);
        var response = await httpClient.PostAsync(endpoint, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TResponse>(content)!;
    }

    private async Task<SearchResponse> PostValidatedRequest<TBody, TResponse>(string endpoint, TBody body)
    {
        var json = JsonConvert.SerializeObject(body);
        var response = await httpClient.PostAsync(endpoint, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var failures = JsonConvert.DeserializeObject<List<ValidationFailureResponse>>(content) ?? [];
            return SearchResponse.Failed(failures);
        }

        response.EnsureSuccessStatusCode();
        var result = JsonConvert.DeserializeObject<TResponse>(content)!;
        return SearchResponse.Succeeded((SearchInitiationResponse)(object)result);
    }
}

internal class SearchResponse
{
    public bool WasSuccess { get; private init; }
    public SearchInitiationResponse? Response { get; private init; }
    public IReadOnlyCollection<ValidationFailureResponse> ValidationFailures { get; private init; } = [];

    public static SearchResponse Succeeded(SearchInitiationResponse response) =>
        new() { WasSuccess = true, Response = response };

    public static SearchResponse Failed(List<ValidationFailureResponse> failures) =>
        new() { WasSuccess = false, ValidationFailures = failures };
}

internal class ValidationFailureResponse
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
}
