using EdgeAnalytics.Worker;
using EdgeAnalytics.Application;
using EdgeAnalytics.Abstractions.Extract;
using EdgeAnalytics.Abstractions.Load;
using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Infrastructure.Extract.Sportsbook.DraftKings;
using EdgeAnalytics.Infrastructure.Load;
using EdgeAnalytics.Pipelines.CollegeBasketball.Sportsbook.DraftKings.v1;
using EdgeAnalytics.Infrastructure.Transform.Normalize;
using EdgeAnalytics.Abstractions.Transform;
using EdgeAnalytics.Infrastructure.Pipeline;
using EdgeAnalytics.Application.Execution;
using EdgeAnalytics.Infrastructure.Extract.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using EdgeAnalytics.Infrastructure.Extract.Consensus.ScoresAndOdds;
using EdgeAnalytics.Infrastructure.Load.File;
using EdgeAnalytics.Pipelines.CollegeBasketball.Consensus;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "EdgeAnalytics.Worker",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddHttpClientInstrumentation()
            .AddSource("EdgeAnalytics.Pipelines")
            .AddSource("EdgeAnalytics.Scraping")
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddRuntimeInstrumentation()
            .AddConsoleExporter();
    });

// Resolve and log data root
var dataRootSetting = builder.Configuration["Data:Root"] ?? "data";
var dataRootPath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, dataRootSetting)
);
builder.Logging.Services.BuildServiceProvider()
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Startup")
    .LogInformation("Resolved DataRoot: {Path}", dataRootPath);
builder.Services.AddSingleton(new DataRoot(dataRootPath));

builder.Services.AddHostedService<PipelineHostedService>();
builder.Services.AddSingleton<IWatermarkStore, FileWatermarkStore>();
builder.Services.AddSingleton<IContextualExtractor<string>, DraftKingsOddsScraper>();
builder.Services.AddSingleton<ILoader<string>, ConsoleLoader>();
builder.Services.AddSingleton<PipelineRunner>();
builder.Services.AddSingleton<ITransformer<object, object>, UppercaseTransformer>();
builder.Services.Configure<PipelineScheduleOptions>(builder.Configuration);
builder.Services.AddSingleton<PipelineRetryPolicyFactory>();
builder.Services.AddSingleton<IPipeline, DraftKingsOddsPipeline>();

// Http clients
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IScraper, HttpScraper>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "EdgeAnalyticsBot/1.0 (+contact@example.com)");
});

// ScoresAndOdds
builder.Services.AddHttpClient<ScoresAndOddsConsensusExtractor>();
builder.Services.AddSingleton<IContextualExtractor<string>, ScoresAndOddsConsensusExtractor>();
builder.Services.AddSingleton<ScoresAndOddsConsensusParser>();
builder.Services.AddSingleton<DedupingNdjsonSink<ScoresAndOddsSpreadMarketState>>();
builder.Services.AddSingleton<IPipeline, ScoresAndOddsConsensusPipeline>();

var host = builder.Build();
host.Run();