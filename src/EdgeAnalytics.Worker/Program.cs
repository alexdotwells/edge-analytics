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

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<PipelineHostedService>();
builder.Services.AddSingleton<IWatermarkStore, FileWatermarkStore>();
builder.Services.AddSingleton<IContextualExtractor<string>, DraftKingsOddsScraper>();
builder.Services.AddSingleton<ILoader<string>, ConsoleLoader>();
builder.Services.AddSingleton<IPipeline, DraftKingsOddsPipeline>();
builder.Services.AddSingleton<PipelineRunner>();
builder.Services.AddSingleton<ITransformer<object, object>, UppercaseTransformer>();

var host = builder.Build();

// ---- PROOF OF LIFE (TEMP) ----
// using (var scope = host.Services.CreateScope())
// {
//     var runner = scope.ServiceProvider.GetRequiredService<PipelineRunner>();
//     await runner.RunAsync("cbb-draftkings-odds-v1", CancellationToken.None);
// }
// --------------------------------------------

host.Run();