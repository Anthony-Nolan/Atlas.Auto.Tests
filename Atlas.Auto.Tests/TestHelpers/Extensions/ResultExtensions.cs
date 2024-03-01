using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Atlas.Auto.Tests.TestHelpers.Extensions;

internal static class ResultExtensions
{
    public static MatchingAlgorithmResult? GetMatchingResult(this OriginalMatchingAlgorithmResultSet resultSet, string donorCode) 
        => resultSet.Results.SingleOrDefault(r => r.DonorCode == donorCode);

    public static string Serialize<TResult>(this TResult result) where TResult : Result
    {
        var resultAsJson = JsonConvert.SerializeObject(result, Formatting.Indented);
        var token = JObject.Parse(resultAsJson);
        return token.ToString();
    }
}