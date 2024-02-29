using System.Collections.Concurrent;
using System.Reflection;

namespace Atlas.Auto.Tests.TestHelpers.Services
{
    internal static class SourceDataReader
    {
        private const string SourceDataFilePath = "Atlas.Auto.Tests.TestHelpers.SourceData.";

        /// <summary>
        /// Key is file name, value is file contents
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> PreviouslyLoadedFiles = new();

        public static async Task<string> ReadJsonFile(string fileName)
        {
            return PreviouslyLoadedFiles.GetOrAdd(fileName, await LoadFile(fileName));
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
