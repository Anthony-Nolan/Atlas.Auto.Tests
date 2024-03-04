using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Atlas.Auto.Tests.TestHelpers.Extensions;

internal static class JsonExtensions
{
    public static string SerializeSingle<T>(this T source) => source.Serialize(JObject.Parse);

    public static string SerializeCollection<T>(this IEnumerable<T> source) => source.Serialize(JArray.Parse);

    private static string Serialize<T>(this T source, Func<string, JToken> parseFunc)
    {
        var serializedObject = JsonConvert.SerializeObject(source, Formatting.Indented);
        var token = parseFunc(serializedObject);
        return token.ToString();
    }
}