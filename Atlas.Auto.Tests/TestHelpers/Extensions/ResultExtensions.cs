﻿using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Atlas.Auto.Tests.TestHelpers.Extensions;

internal static class ResultExtensions
{
    public static MatchingAlgorithmResult? GetDonorResult(this OriginalMatchingAlgorithmResultSet resultSet, string donorCode) 
        => resultSet.GetDonorResult<OriginalMatchingAlgorithmResultSet, MatchingAlgorithmResult>(donorCode);

    public static SearchResult? GetDonorResult(this OriginalSearchResultSet resultSet, string donorCode)
        => resultSet.GetDonorResult<OriginalSearchResultSet, SearchResult>(donorCode);

    private static TResult? GetDonorResult<TSet, TResult>(this TSet resultSet, string donorCode)
        where TSet: ResultSet<TResult> where TResult : Result
        => resultSet.Results.SingleOrDefault(r => r.DonorCode == donorCode);

    public static string Serialize<TResult>(this TResult result) where TResult : Result
    {
        var resultAsJson = JsonConvert.SerializeObject(result, Formatting.Indented);
        var token = JObject.Parse(resultAsJson);
        return token.ToString();
    }
}