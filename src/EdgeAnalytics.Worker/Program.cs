using EdgeAnalytics.Worker;
using EdgeAnalytics.Application;
using EdgeAnalytics.Abstractions.Extract;
using EdgeAnalytics.Abstractions.Load;
using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Infrastructure.Extract.Sportsbook.DraftKings;
using EdgeAnalytics.Infrastructure.Load;
using EdgeAnalytics.Pipelines.CollegeBasketball.Sportsbook.DraftKings.v1;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IExtractor<string>, DraftKingsOddsExtractor>();
builder.Services.AddSingleton<ILoader<string>, ConsoleLoader>();
builder.Services.AddSingleton<IPipeline, DraftKingsOddsPipeline>();
builder.Services.AddSingleton<PipelineRunner>();

var host = builder.Build();
host.Run();