using Polly;
using Microsoft.Extensions.Logging;
using EdgeAnalytics.Domain.Common;

namespace EdgeAnalytics.Application.Execution;

public sealed class PipelineRetryPolicyFactory
{
    private readonly ILogger<PipelineRetryPolicyFactory> _logger;

    public PipelineRetryPolicyFactory(
        ILogger<PipelineRetryPolicyFactory> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy Create()
    {
        return Policy
            .Handle<PipelineFailureException>(ex =>
                ex.FailureType == PipelineFailureType.Transient)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {Attempt} after {Delay}",
                        attempt,
                        delay);
                });
    }
}
