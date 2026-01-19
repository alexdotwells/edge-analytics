using EdgeAnalytics.Application;
using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Domain.Common;
using Microsoft.Extensions.Options;

namespace EdgeAnalytics.Worker;

public sealed class PipelineHostedService : BackgroundService
{
    private readonly PipelineRunner _runner;
    private readonly ILogger<PipelineHostedService> _logger;
    private readonly PipelineScheduleOptions _options;

    public PipelineHostedService(
        PipelineRunner runner,
        IOptions<PipelineScheduleOptions> options,
        ILogger<PipelineHostedService> logger)
    {
        _runner = runner;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PipelineHostedService starting");

        var tasks = _options.Pipelines
            .Where(p => p.Policy != PipelineExecutionPolicy.Manual)
            .Select(p => RunPipelineAsync(p, stoppingToken));

        await Task.WhenAll(tasks);
    }

    private async Task RunPipelineAsync(
        PipelineSchedule schedule,
        CancellationToken ct)
    {
        if (schedule.Policy == PipelineExecutionPolicy.OnStartup)
        {
            await _runner.RunAsync(
                schedule.PipelineName,
                Sport.CollegeBasketball,
                ct);
            return;
        }

        if (schedule.Policy == PipelineExecutionPolicy.Scheduled &&
            schedule.Interval.HasValue)
        {
            while (!ct.IsCancellationRequested)
            {
                await _runner.RunAsync(
                    schedule.PipelineName,
                    Sport.CollegeBasketball,
                    ct);

                await Task.Delay(schedule.Interval.Value, ct);
            }
        }
    }
}
