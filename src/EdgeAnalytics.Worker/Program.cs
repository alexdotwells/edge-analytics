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
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<PipelineHostedService>();
builder.Services.AddSingleton<IWatermarkStore, FileWatermarkStore>();
builder.Services.AddSingleton<IContextualExtractor<string>, DraftKingsOddsScraper>();
builder.Services.AddSingleton<ILoader<string>, ConsoleLoader>();
builder.Services.AddSingleton<PipelineRunner>();
builder.Services.AddSingleton<ITransformer<object, object>, UppercaseTransformer>();
builder.Services.Configure<PipelineScheduleOptions>(builder.Configuration);
builder.Services.AddSingleton<PipelineRetryPolicyFactory>();
builder.Services.AddSingleton<IPipeline, DraftKingsOddsPipeline>();

builder.Services.AddHttpClient<IScraper, HttpScraper>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "EdgeAnalyticsBot/1.0 (+contact@example.com)");
});

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
    
var host = builder.Build();
host.Run();