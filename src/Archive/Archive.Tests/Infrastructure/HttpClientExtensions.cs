using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Archive.Tests.Infrastructure;

public static class HttpClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static Task<HttpResponseMessage> UploadAsync(this HttpClient client, string cabinet, string field, byte[] zip, string fileName) =>
        client.PostAsync($"cabinets/{cabinet}/uploads", new MultipartFormDataContent
        {
            { new StringContent(field), "field" },
            { new ByteArrayContent(zip), "file", fileName }
        });

    public static Task<HttpResponseMessage> PutIndexValueAsync(this HttpClient client, Guid id, string field, string valueJson) =>
        client.PutAsync($"documents/{id}/index-values/{field}",
            new StringContent($$"""{"value":{{valueJson}}}""", Encoding.UTF8, "application/json"));

    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }
}