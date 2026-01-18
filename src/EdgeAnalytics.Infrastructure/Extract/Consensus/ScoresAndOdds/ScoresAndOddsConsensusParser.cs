using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using EdgeAnalytics.Domain.Common;

namespace EdgeAnalytics.Infrastructure.Extract.Consensus.ScoresAndOdds;

public sealed record ScoresAndOddsSpreadConsensus(
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset GameStartUtc,
    string AwayTeam,
    string HomeTeam,
    decimal HomeSpread,                // e.g., +6.5
    int AwayBetsPct,
    int HomeBetsPct,
    int AwayHandlePct,
    int HomeHandlePct,
    int? AwayOdds,                     // e.g., -102 (optional)
    int? HomeOdds                      // e.g., -110 (optional)
);

public sealed class ScoresAndOddsConsensusParser
{
    private readonly HtmlParser _parser = new();

    public async Task<IReadOnlyList<ScoresAndOddsSpreadConsensus>> ParseSpreadAsync(
        string html,
        DateTimeOffset observedAtUtc,
        CancellationToken ct)
    {
        var doc = await _parser.ParseDocumentAsync(html, ct);

        // Each game appears to be a card like:
        // <div class="trend-card consensus consensus-table-spread--0 active" ...>
        var cards = doc.QuerySelectorAll("div.trend-card.consensus");

        var results = new List<ScoresAndOddsSpreadConsensus>();

        foreach (var card in cards)
        {
            // Teams
            var leftTeam = card.QuerySelector(".event-header .team-pennant.left .team-name span")?.TextContent?.Trim();
            var rightTeam = card.QuerySelector(".event-header .team-pennant.right .team-name span")?.TextContent?.Trim();

            if (string.IsNullOrWhiteSpace(leftTeam) || string.IsNullOrWhiteSpace(rightTeam))
                continue;

            // Time: prefer the machine-readable UTC value
            var utcValue = card.QuerySelector(".event-info [data-role='localtime']")?.GetAttribute("data-value");
            if (!DateTimeOffset.TryParse(utcValue, out var gameStartUtc))
            {
                // If this fails, we can still proceed (but ideally log and skip)
                continue;
            }

            // Spread + percentages live under ".trend-graphs li.consensus.active"
            var consensusBlock = card.QuerySelector(".module-body ul.trend-graphs li.consensus.active");
            if (consensusBlock is null) continue;

            // Extract the two sides + their spreads from:
            // <strong> ILST (-6.5) </strong> ... <strong> INST (+6.5) </strong>
            var strongs = consensusBlock.QuerySelectorAll(".trend-graph-sides strong")
                .Select(s => s.TextContent.Trim())
                .ToArray();

            if (strongs.Length < 2) continue;

            // strong text looks like "ILST (-6.5)" and "INST (+6.5)"
            // We want the numeric parts.
            var awaySpread = TryParseParenDecimal(strongs[0]); // -6.5
            var homeSpread = TryParseParenDecimal(strongs[1]); // +6.5

            if (awaySpread is null || homeSpread is null)
                continue;

            // Percent blocks:
            // First .trend-graph-percentage => Bets
            // Second .trend-graph-percentage => Money
            var pctBlocks = consensusBlock.QuerySelectorAll(".trend-graph-percentage").ToArray();
            if (pctBlocks.Length < 2) continue;

            var (awayBets, homeBets) = ParseTwoPercents(pctBlocks[0]);
            var (awayMoney, homeMoney) = ParseTwoPercents(pctBlocks[1]);

            if (awayBets is null || homeBets is null || awayMoney is null || homeMoney is null)
                continue;

            // Best odds (optional)
            // "Best away Odds" => first ".best-odds-container"
            // "Best home Odds" => second ".best-odds-container"
            int? bestAwayOdds = null;
            int? bestHomeOdds = null;

            var bestOddsContainers = consensusBlock.QuerySelectorAll(".best-odds-container").ToArray();
            if (bestOddsContainers.Length >= 2)
            {
                bestAwayOdds = TryParseSignedInt(bestOddsContainers[0].QuerySelector(".data-odds")?.TextContent);
                bestHomeOdds = TryParseSignedInt(bestOddsContainers[1].QuerySelector(".data-odds")?.TextContent);
            }

            // Convention: store HomeSpread, and assume AwaySpread = -HomeSpread
            // In your example: HomeSpread = +6.5
            results.Add(new ScoresAndOddsSpreadConsensus(
                ObservedAtUtc: observedAtUtc,
                GameStartUtc: gameStartUtc,
                AwayTeam: leftTeam,
                HomeTeam: rightTeam,
                HomeSpread: homeSpread.Value,
                AwayBetsPct: awayBets.Value,
                HomeBetsPct: homeBets.Value,
                AwayHandlePct: awayMoney.Value,
                HomeHandlePct: homeMoney.Value,
                AwayOdds: bestAwayOdds,
                HomeOdds: bestHomeOdds
            ));
        }

        return results;
    }

    private static (int? a, int? b) ParseTwoPercents(IElement container)
    {
        var aText = container.QuerySelector(".percentage-a")?.TextContent?.Trim().Replace("%", "");
        var bText = container.QuerySelector(".percentage-b")?.TextContent?.Trim().Replace("%", "");

        return (TryParseInt(aText), TryParseInt(bText));
    }

    private static decimal? TryParseParenDecimal(string text)
    {
        // expects something like "ILST (-6.5)" or "INST (+6.5)"
        var open = text.IndexOf('(');
        var close = text.IndexOf(')');

        if (open < 0 || close < 0 || close <= open) return null;

        var inside = text.Substring(open + 1, close - open - 1).Trim();
        return decimal.TryParse(inside, out var value) ? value : null;
    }

    private static int? TryParseInt(string? text)
        => int.TryParse(text, out var v) ? v : null;

    private static int? TryParseSignedInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        return int.TryParse(text, out var v) ? v : null;
    }
}
