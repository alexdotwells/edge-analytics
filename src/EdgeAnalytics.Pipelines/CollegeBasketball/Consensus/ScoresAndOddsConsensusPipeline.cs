using EdgeAnalytics.Abstractions.Extract;
using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Infrastructure.Extract.Consensus.ScoresAndOdds;
using EdgeAnalytics.Infrastructure.Load.File;
using Microsoft.Extensions.Logging;

namespace EdgeAnalytics.Pipelines.CollegeBasketball.Consensus;

public sealed class ScoresAndOddsConsensusPipeline : IPipeline
{
    public string Name => "cbb-scoresandodds-consensus-spread-v1";

    private readonly IContextualExtractor<string> _extractor;
    private readonly ScoresAndOddsConsensusParser _parser;
    private readonly DedupingNdjsonSink<ScoresAndOddsSpreadMarketState> _sink;
    private readonly ILogger<ScoresAndOddsConsensusPipeline> _logger;

    public ScoresAndOddsConsensusPipeline(
        IContextualExtractor<string> extractor,
        ScoresAndOddsConsensusParser parser,
        DedupingNdjsonSink<ScoresAndOddsSpreadMarketState> sink,
        ILogger<ScoresAndOddsConsensusPipeline> logger)
    {
        _extractor = extractor;
        _parser = parser;
        _sink = sink;
        _logger = logger;
    }

    public async Task RunAsync(PipelineContext context, CancellationToken ct)
    {
        _logger.LogInformation("Pipeline execution started");

        var html = await _extractor.ExtractAsync(context, ct);

        var observedAtUtc = DateTimeOffset.UtcNow;

        // Parse spread consensus rows
        var parsed = await _parser.ParseSpreadAsync(html, observedAtUtc, ct);

        // Map to our stored state model
        var states = parsed.Select(x => new ScoresAndOddsSpreadMarketState(
            ObservedAtUtc: x.ObservedAtUtc,
            GameStartUtc: x.GameStartUtc,
            AwayTeam: x.AwayTeam,
            HomeTeam: x.HomeTeam,
            HomeSpread: x.HomeSpread,
            AwayBetsPct: x.AwayBetsPct,
            HomeBetsPct: x.HomeBetsPct,
            AwayHandlePct: x.AwayHandlePct,
            HomeHandlePct: x.HomeHandlePct,
            AwayBestOdds: x.AwayOdds,
            HomeBestOdds: x.HomeOdds,
            Source: "ScoresAndOdds"
        )).ToList();

        // Persist only changes    
        static string IdentityKey(ScoresAndOddsSpreadMarketState s)
            => $"{s.GameStartUtc:O}|{s.AwayTeam}|{s.HomeTeam}|spread";
            //TODO: team IDs to improve gamekey
            //=> $"{s.GameStartUtc:O}|{s.AwayTeamId}|{s.HomeTeamId}|spread";

        static string Fingerprint(ScoresAndOddsSpreadMarketState s)
        {
            // fingerprint = what constitutes a "change"
            var input = string.Join("|",
                s.HomeSpread,
                s.AwayBetsPct, s.HomeBetsPct,
                s.AwayHandlePct, s.HomeHandlePct,
                s.AwayBestOdds?.ToString() ?? "null",
                s.HomeBestOdds?.ToString() ?? "null"
            );

            return DedupingNdjsonSink<ScoresAndOddsSpreadMarketState>.Sha256(input);
        }

        var day = observedAtUtc.ToString("yyyy-MM-dd");
        var baseDir = Path.Combine("curated", "cbb", "consensus", "scoresandodds", "spread");
        var ndjsonRel = Path.Combine(baseDir, $"{day}.ndjson");
        var indexRel = Path.Combine(baseDir, $"{day}.index.json");

        var appended = await _sink.AppendChangesAsync(
            ndjsonRelativePath: ndjsonRel,
            indexRelativePath: indexRel,
            items: states,
            identityKey: IdentityKey,
            changeFingerprint: Fingerprint,
            ct: ct,
            ensureFileExistsEvenIfNoChanges: true);

        _logger.LogInformation("Pipeline execution finished. Changes appended: {Count}", appended);
    }
}
