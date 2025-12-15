using System.Net;
using EdgeAnalytics.Abstractions.Extract;
using EdgeAnalytics.Domain.Common;

namespace EdgeAnalytics.Infrastructure.Extract.Web;

public sealed class HttpScraper : IScraper
{
    private readonly HttpClient _client;
    private readonly SemaphoreSlim _throttle = new(1, 1);

    public HttpScraper(HttpClient client)
    {
        _client = client;
    }

    public async Task<string> FetchAsync(Uri uri, CancellationToken ct)
    {
        var response = await _client.GetAsync(uri, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests ||
            (int)response.StatusCode >= 500)
        {
            throw new PipelineFailureException(
                PipelineFailureType.Transient,
                $"Remote host returned {response.StatusCode}");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new PipelineFailureException(
                PipelineFailureType.Permanent,
                "Endpoint not found (404)");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new PipelineFailureException(
                PipelineFailureType.Fatal,
                $"Unexpected HTTP status {response.StatusCode}");
        }

        return await response.Content.ReadAsStringAsync(ct);

    }
}
