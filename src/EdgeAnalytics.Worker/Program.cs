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

var host = builder.Build();
host.Run();