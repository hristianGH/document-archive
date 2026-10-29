using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Models.Exports;

namespace Export.Tests.Infrastructure;

public static class HttpClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static Task<HttpResponseMessage> ExportAsync(this HttpClient client, string cabinet, string field, string from, string to) =>
        client.PostAsJsonAsync("exports", NewRequest(cabinet, field, from, to));

    // Returns once the headers arrive, while the zip may still be streaming.
    public static Task<HttpResponseMessage> StartExportAsync(this HttpClient client, string cabinet, string field, string from, string to) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "exports") { Content = JsonContent.Create(NewRequest(cabinet, field, from, to)) },
            HttpCompletionOption.ResponseHeadersRead);

    public static async Task<IReadOnlyList<ZipItem>> ReadZipAsync(this HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        using var zip = new ZipArchive(new MemoryStream(await response.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
        return [.. zip.Entries.Select(e => new ZipItem(e.FullName, ReadText(e)))];
    }

    private static ExportRequest NewRequest(string cabinet, string field, string from, string to) =>
        new() { Cabinet = cabinet, Field = field, From = from, To = to };

    private static string ReadText(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}

public record ZipItem(string Name, string Content);