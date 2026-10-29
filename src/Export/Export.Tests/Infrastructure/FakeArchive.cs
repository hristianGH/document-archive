using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Shared.Models.Cabinets;
using Shared.Models.Documents;
using Shared.Models.Paging;

namespace Export.Tests.Infrastructure;

// The Archive's HTTP API in memory: same routes, paging cap, range filter, order and ProblemDetails errors.
public class FakeArchive : HttpMessageHandler
{
    private static readonly TimeSpan EndOfDay = new(23, 59, 59);

    private static readonly TimeSpan WaitingLimit = TimeSpan.FromSeconds(5);

    private readonly List<FakeDocument> documents = [];

    private readonly List<string> requests = [];

    private readonly List<(Guid Id, string Field, string Value)> marks = [];

    private readonly SemaphoreSlim waiting = new(0);

    public int MaxPageSize { get; set; } = 2;

    // Replaces the Archive's answer (or throws) for the requests it returns a response for; null lets the Archive answer.
    public Func<HttpRequestMessage, HttpResponseMessage?>? Override { get; set; }

    // Delays the answer until the returned task completes or the request is cancelled; null answers at once.
    public Func<HttpRequestMessage, Task?>? Hold { get; set; }

    public IReadOnlyList<string> Requests
    {
        get
        {
            lock (requests)
            {
                return [.. requests];
            }
        }
    }

    public IReadOnlyList<(Guid Id, string Field, string Value)> Marks
    {
        get
        {
            lock (marks)
            {
                return [.. marks];
            }
        }
    }

    public FakeDocument Add(string cabinet, string title, DateTime storedAt, params (string FileName, string Content)[] parts)
    {
        var document = new FakeDocument
        {
            Cabinet = cabinet,
            Title = title,
            StoredAt = storedAt,
            Parts = [.. parts.Select((p, i) => new FakePart { Position = i, FileName = p.FileName, Content = Encoding.UTF8.GetBytes(p.Content) })]
        };
        documents.Add(document);
        return document;
    }

    // The Invoices documents stored from 2026-10-01 to 2026-10-28, in export order, plus documents just outside the range and in Contracts.
    public IReadOnlyList<FakeDocument> SeedStandard()
    {
        IReadOnlyList<FakeDocument> inRange =
        [
            Add("Invoices", "invoice-001.txt", Utc(2026, 10, 1, 0, 0, 0), ("scan.txt", "first")),
            Add("Invoices", "Фактури Q1", Utc(2026, 10, 5, 12, 0, 0), ("януари.txt", "Януари"), ("февруари.txt", "Февруари"), ("март.txt", "Март")),
            Add("Invoices", "empty-folder", Utc(2026, 10, 10, 12, 0, 0)),
            Add("Invoices", "invoice-001.txt", Utc(2026, 10, 20, 12, 0, 0), ("scan.txt", "second")),
            Add("Invoices", "Ünï 名前.txt", Utc(2026, 10, 28, 23, 59, 59), ("scan.txt", "Ünï 名前"))
        ];
        Add("Invoices", "before.txt", Utc(2026, 9, 30, 23, 59, 59), ("scan.txt", "before"));
        Add("Invoices", "after.txt", Utc(2026, 10, 29, 0, 0, 0), ("scan.txt", "after"));
        Add("Contracts", "contract.txt", Utc(2026, 10, 15, 12, 0, 0), ("scan.txt", "contract"));
        return inRange;
    }

    public static DateTime Utc(int year, int month, int day, int hour, int minute, int second) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    public int CountRequests(string prefix) => Requests.Count(r => r.StartsWith(prefix, StringComparison.Ordinal));

    // Completes once a request is held or a part's content stalls; fails after 5 s so a broken test does not hang.
    public async Task WaitingAsync()
    {
        if (!await waiting.WaitAsync(WaitingLimit))
        {
            throw new TimeoutException($"Nothing waited on the Archive within {WaitingLimit}.");
        }
    }

    public static HttpResponseMessage Problem(HttpStatusCode status, string detail) => new(status)
    {
        Content = JsonContent.Create(new { status = (int)status, detail }, new MediaTypeHeaderValue("application/problem+json"))
    };

    public static HttpResponseMessage BreakingContent(string sentBeforeBreaking) => new(HttpStatusCode.OK)
    {
        Content = new BreakingHttpContent(Encoding.UTF8.GetBytes(sentBeforeBreaking))
    };

    public HttpResponseMessage StallingContent(string sentBeforeStalling) => new(HttpStatusCode.OK)
    {
        Content = new StallingHttpContent(Encoding.UTF8.GetBytes(sentBeforeStalling), () => waiting.Release())
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        lock (requests)
        {
            requests.Add($"{request.Method} {Uri.UnescapeDataString(uri.PathAndQuery)}");
        }

        if (Hold?.Invoke(request) is { } hold)
        {
            waiting.Release();
            await hold.WaitAsync(cancellationToken);
        }

        if (Override?.Invoke(request) is { } replaced)
        {
            return replaced;
        }

        string[] path = [.. uri.AbsolutePath.Trim('/').Split('/').Select(Uri.UnescapeDataString)];
        return (request.Method.Method, path) switch
        {
            ("GET", ["cabinets", var cabinet]) => GetCabinet(cabinet),
            ("GET", ["cabinets", var cabinet, "documents"]) => GetPage(cabinet, QueryHelpers.ParseQuery(uri.Query)),
            ("GET", ["documents", var id]) => GetDocument(Guid.Parse(id)),
            ("GET", ["documents", var id, "parts", var partId, "content"]) => GetContent(Guid.Parse(id), Guid.Parse(partId)),
            ("PUT", ["documents", var id, "index-values", var field]) => await SetIndexValueAsync(Guid.Parse(id), field, request, cancellationToken),
            _ => Problem(HttpStatusCode.NotFound, "No such route.")
        };
    }

    private static HttpResponseMessage GetCabinet(string name) =>
        FindCabinet(name) is { } cabinet ? Json(cabinet) : Problem(HttpStatusCode.NotFound, $"Cabinet '{name}' was not found.");

    private HttpResponseMessage GetPage(string name, Dictionary<string, StringValues> query)
    {
        if (FindCabinet(name) is not { } cabinet)
        {
            return Problem(HttpStatusCode.NotFound, $"Cabinet '{name}' was not found.");
        }

        if (!TryParseInstant(query.GetValueOrDefault("from").ToString(), TimeSpan.Zero, out var from))
        {
            return Problem(HttpStatusCode.BadRequest, "'from' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).");
        }

        if (!TryParseInstant(query.GetValueOrDefault("to").ToString(), EndOfDay, out var to))
        {
            return Problem(HttpStatusCode.BadRequest, "'to' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).");
        }

        if (from > to)
        {
            return Problem(HttpStatusCode.BadRequest, "'from' must not be later than 'to'.");
        }

        var page = int.Parse(query["page"].ToString(), CultureInfo.InvariantCulture);
        var pageSize = Math.Min(int.Parse(query["pageSize"].ToString(), CultureInfo.InvariantCulture), MaxPageSize);
        var matching = documents
            .Where(d => d.Cabinet == cabinet.Name && d.StoredAt >= from && d.StoredAt <= to)
            .OrderBy(d => d.StoredAt).ThenBy(d => d.Id)
            .ToList();

        return Json(new PagedResponse<DocumentListItemResponse>
        {
            Items = [.. matching.Skip((page - 1) * pageSize).Take(pageSize).Select(d => d.ToListItem())],
            Page = page,
            PageSize = pageSize,
            TotalCount = matching.Count
        });
    }

    private HttpResponseMessage GetDocument(Guid id) =>
        Find(id) is { } document ? Json(document.ToResponse()) : Problem(HttpStatusCode.NotFound, $"Document '{id}' was not found.");

    private HttpResponseMessage GetContent(Guid id, Guid partId) =>
        Find(id)?.Parts.FirstOrDefault(p => p.Id == partId) is { } part
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(part.Content) }
            : Problem(HttpStatusCode.NotFound, $"Part '{partId}' was not found.");

    private async Task<HttpResponseMessage> SetIndexValueAsync(Guid id, string field, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Find(id) is not { } document)
        {
            return Problem(HttpStatusCode.NotFound, $"Document '{id}' was not found.");
        }

        var body = await request.Content!.ReadFromJsonAsync<SetIndexValueRequest>(HttpClientExtensions.JsonOptions, cancellationToken);
        var value = body!.Value.GetString()!;
        document.IndexValues[field] = value;
        lock (marks)
        {
            marks.Add((id, field, value));
        }

        return Json(document.ToResponse());
    }

    private FakeDocument? Find(Guid id) => documents.FirstOrDefault(d => d.Id == id);

    private static CabinetResponse? FindCabinet(string name) =>
        TestFiles.Cabinets.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private static bool TryParseInstant(string value, TimeSpan dateOnlyTime, out DateTime utc)
    {
        const DateTimeStyles utcStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, utcStyles, out utc))
        {
            utc += dateOnlyTime;
            return true;
        }

        return DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, utcStyles, out utc);
    }

    private static HttpResponseMessage Json<T>(T body) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(body, options: HttpClientExtensions.JsonOptions)
    };

    private sealed class BreakingHttpContent(byte[] sentBeforeBreaking) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await stream.WriteAsync(sentBeforeBreaking);
            throw new IOException("The connection to the Archive broke.");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class StallingHttpContent(byte[] sentBeforeStalling, Action stalled) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (sentBeforeStalling.Length > 0)
            {
                await stream.WriteAsync(sentBeforeStalling, cancellationToken);
            }

            stalled();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}