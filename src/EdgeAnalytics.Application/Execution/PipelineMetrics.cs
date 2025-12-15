using System.Diagnostics.Metrics;

namespace EdgeAnalytics.Application.Execution;

public static class PipelineMetrics
{
    public static readonly Meter Meter =
        new("EdgeAnalytics.Execution");

    public static readonly Counter<long> Runs =
        Meter.CreateCounter<long>("pipeline.runs");

    public static readonly Counter<long> Failures =
        Meter.CreateCounter<long>("pipeline.failures");
}
