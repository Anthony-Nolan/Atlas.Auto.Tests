using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services
{
    internal static class SourceDataReader
    {
        private const string SourceDataFilePath = "Atlas.Auto.Tests.TestHelpers.SourceData.";

        private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> PreviouslyLoadedFiles = new();

        public static async Task<T> ReadJsonFile<T>(string fileName)
        {
            var lazyFileContents = PreviouslyLoadedFiles.GetOrAdd(fileName, _ => new Lazy<Task<string>>(() => LoadFile(fileName)));
            var fileContents = await lazyFileContents.Value;
            var result = JsonSerializer.Deserialize<T>(fileContents);
            return result ?? throw new InvalidOperationException($"Failed to load file {fileName}");
        }

        private static async Task<string> LoadFile(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = SourceDataFilePath + fileName;

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream ?? throw new InvalidOperationException());
            return await reader.ReadToEndAsync();
        }
    }
}
