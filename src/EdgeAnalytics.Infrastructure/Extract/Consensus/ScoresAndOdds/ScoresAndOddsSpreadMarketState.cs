namespace EdgeAnalytics.Infrastructure.Extract.Consensus.ScoresAndOdds;

public sealed record ScoresAndOddsSpreadMarketState(
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset GameStartUtc,
    string AwayTeam,
    string HomeTeam,
    decimal HomeSpread, // +6.5 means home is +6.5 (away -6.5)
    int AwayBetsPct,
    int HomeBetsPct,
    int AwayHandlePct,
    int HomeHandlePct,
    int? AwayBestOdds,
    int? HomeBestOdds,
    string Source // "ScoresAndOdds"
);
