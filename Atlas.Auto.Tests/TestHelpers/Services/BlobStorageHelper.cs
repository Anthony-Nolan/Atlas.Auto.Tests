using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class BlobStorageHelper(BlobServiceClient blobClient, BlobStorageSettings settings)
{
    private static readonly JsonSerializer Serializer = CreateSerializer();

    private static JsonSerializer CreateSerializer()
    {
        var s = new JsonSerializer();
        s.Converters.Add(new StringEnumConverter());
        return s;
    }

    public async Task UploadDonorFile(object fileContents, string fileName)
    {
        var container = blobClient.GetBlobContainerClient(settings.DonorFileContainer);
        using var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        await using (var jsonWriter = new JsonTextWriter(writer))
        {
            Serializer.Serialize(jsonWriter, fileContents);
            await jsonWriter.FlushAsync();
        }

        stream.Position = 0;
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" }
        };
        await container.GetBlobClient(fileName).UploadAsync(stream, options);
    }

    public async Task<TResultSet> DownloadResultSet<TResultSet, TResult>(
        string containerName, string resultFileName, string? batchFolderName)
        where TResultSet : ResultSet<TResult>
        where TResult : Result
    {
        var container = blobClient.GetBlobContainerClient(containerName);
        var resultSet = await DownloadBlob<TResultSet>(container, resultFileName);

        resultSet.Results ??= !string.IsNullOrEmpty(batchFolderName)
            ? await DownloadFolderContents<TResult>(container, batchFolderName)
            : new List<TResult>();

        return resultSet;
    }

    private static async Task<T> DownloadBlob<T>(BlobContainerClient container, string blobName)
    {
        var blob = container.GetBlobClient(blobName);
        var download = await blob.DownloadContentAsync();
        using var stream = download.Value.Content.ToStream();
        using var reader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(reader);
        return Serializer.Deserialize<T>(jsonReader)
               ?? throw new InvalidOperationException($"Failed to deserialize blob '{blobName}' from container '{container.Name}'.");
    }

    private static async Task<List<TResult>> DownloadFolderContents<TResult>(
        BlobContainerClient container, string folderName)
    {
        var data = new List<TResult>();
        await foreach (var blobItem in container.GetBlobsAsync(prefix: $"{folderName}/"))
        {
            var items = await DownloadBlob<IEnumerable<TResult>>(container, blobItem.Name);
            data.AddRange(items);
        }
        return data;
    }
}
