using EdgeAnalytics.Abstractions.Extract;
using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Domain.Common;
using Microsoft.Extensions.Logging;

namespace EdgeAnalytics.Infrastructure.Extract.Consensus.ScoresAndOdds;

public sealed class ScoresAndOddsConsensusExtractor : IContextualExtractor<string>
{
    private static readonly Uri CbbConsensusUri =
        new("https://www.scoresandodds.com/ncaab/consensus-picks");

    private readonly HttpClient _http;
    private readonly ILogger<ScoresAndOddsConsensusExtractor> _logger;

    public ScoresAndOddsConsensusExtractor(
        HttpClient http,
        ILogger<ScoresAndOddsConsensusExtractor> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string> ExtractAsync(PipelineContext context, CancellationToken ct)
    {
        _logger.LogInformation("Fetching ScoresAndOdds consensus HTML: {Url}", CbbConsensusUri);

        using var req = new HttpRequestMessage(HttpMethod.Get, CbbConsensusUri);
        req.Headers.UserAgent.ParseAdd("EdgeAnalytics/1.0 (+https://example.local)");

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "ScoresAndOdds returned {StatusCode}. Body (first 300 chars): {Body}",
                (int)resp.StatusCode,
                body.Length > 300 ? body[..300] : body);

            resp.EnsureSuccessStatusCode();
        }

        return await resp.Content.ReadAsStringAsync(ct);
    }
}
