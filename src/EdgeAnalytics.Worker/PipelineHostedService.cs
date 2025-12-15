using EdgeAnalytics.Application;
using EdgeAnalytics.Domain.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Domain.Common;

namespace EdgeAnalytics.Worker;

public sealed class PipelineHostedService : BackgroundService
{
    private readonly PipelineRunner _runner;
    private readonly ILogger<PipelineHostedService> _logger;

    public PipelineHostedService(
        PipelineRunner runner,
        ILogger<PipelineHostedService> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PipelineHostedService starting");

        try
        {
            await _runner.RunAsync(
                pipelineName: "cbb-draftkings-odds-v1",
                sport: Sport.CollegeBasketball,
                ct: stoppingToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline execution failed");
        }

        _logger.LogInformation("PipelineHostedService finished");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
